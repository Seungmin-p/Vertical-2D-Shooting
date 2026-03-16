using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class Player : MonoBehaviour
{
    public bool isTouchTop;
    public bool isTouchBottom;
    public bool isTouchRight;
    public bool isTouchLeft;

    public int life;
    public int score;
    public float speed;
    public int power;
    public int maxPower;
    public int boom;
    public int maxBoom;
    public float maxShotDelay;
    public float curShotDelay;

    public GameObject bulletObjA;
    public GameObject bulletObjB;
    public GameObject boomEffect;

    public GameManager gameManager;
    public ObjectManager objectManager;

    public bool isHit;
    public bool isBoomTime;

    public GameObject[] followers;
    public bool isRespawnTime;

    private VisualElement m_JoyPanel;
    private List<VisualElement> m_Buttons;
    private float joyH = 0;
    private float joyV = 0;
    private float prevH;
    private bool isButtonAPressed = false;
    private bool isButtonBPressed = false;
    private Button m_ButtonA;
    private Button m_ButtonB;
    
    Animator anim;
    SpriteRenderer spriteRenderer;
    
    void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        Unbeatable();
        Invoke("Unbeatable", 3);
        var uiDoc = FindObjectOfType<UIDocument>();
        if (uiDoc != null)
        {
            m_JoyPanel = uiDoc.rootVisualElement.Q<VisualElement>("JoyPanel");
            m_Buttons = m_JoyPanel.Children().ToList();

            // 2. 패널에 포인터 이벤트 3개 등록! (버튼 9개가 아니라 패널 1개에만 겁니다)
            m_JoyPanel.RegisterCallback<PointerDownEvent>(OnPointerDown);
            m_JoyPanel.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            m_JoyPanel.RegisterCallback<PointerUpEvent>(OnPointerUp);
            m_JoyPanel.RegisterCallback<PointerOutEvent>(OnPointerOut); // 화면 밖으로 나갔을 때 방어용
            
            // 1. A, B 버튼 찾기
            m_ButtonA = uiDoc.rootVisualElement.Q<Button>("Button_A");
            m_ButtonB = uiDoc.rootVisualElement.Q<Button>("Button_B");
            
            if (m_ButtonA != null)
            {
                // 🚨 핵심: TrickleDown.TrickleDown 을 반드시 적어야 버튼이 삼키기 전에 뺏어옵니다!
                m_ButtonA.RegisterCallback<PointerDownEvent>(e => {
                    isButtonAPressed = true;
                }, TrickleDown.TrickleDown);

                m_ButtonA.RegisterCallback<PointerUpEvent>(e => {
                    isButtonAPressed = false;
                }, TrickleDown.TrickleDown);

                m_ButtonA.RegisterCallback<PointerOutEvent>(e => isButtonAPressed = false, TrickleDown.TrickleDown);
            }

            if (m_ButtonB != null)
            {
                m_ButtonB.RegisterCallback<PointerDownEvent>(e => isButtonBPressed = true, TrickleDown.TrickleDown);
                m_ButtonB.RegisterCallback<PointerUpEvent>(e => isButtonBPressed = false, TrickleDown.TrickleDown);
                m_ButtonB.RegisterCallback<PointerOutEvent>(e => isButtonBPressed = false, TrickleDown.TrickleDown);
            }
        }
    }

    void Unbeatable()
    {
        isRespawnTime = !isRespawnTime;
        if (isRespawnTime)
        {
            isRespawnTime = true;
            spriteRenderer.color = new Color(1, 1, 1, 0.5f);

            for (int i = 0; i < followers.Length; i++)
            {
                followers[i].GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0.5f);
            }
        }
        else
        {
            isRespawnTime = false;
            spriteRenderer.color = new Color(1, 1, 1, 1);
            for (int i = 0; i < followers.Length; i++)
            {
                followers[i].GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
            }
        }
    }

    void Update()
    {
        Move();
        Fire();
        Boom();
        Reload();
    }
    
    private void OnPointerDown(PointerDownEvent evt)
    {
        m_JoyPanel.CapturePointer(evt.pointerId); // 터치 권한 꽉 잡기
        CalculateDirection(evt.position);
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        // 누른 채로 드래그할 때만 방향 계산
        if (m_JoyPanel.HasPointerCapture(evt.pointerId))
        {
            CalculateDirection(evt.position);
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        m_JoyPanel.ReleasePointer(evt.pointerId); // 터치 권한 놓아주기
        joyH = 0; // 손을 떼면 무조건 정지!
        joyV = 0;
    }
    
    private void OnPointerOut(PointerOutEvent evt)
    {
        // 마우스나 손가락이 아예 화면 밖으로 튕겨나갔을 때의 안전장치
        joyH = 0;
        joyV = 0;
    }

    // 💡 핵심: 손가락 위치(좌표)를 가지고 9개 버튼 중 어디인지 판별하는 함수
    private void CalculateDirection(Vector2 pointerPos)
    {
        for (int i = 0; i < m_Buttons.Count; i++)
        {
            // 내 손가락이 i번째 버튼 영역 안에 들어왔다면?
            if (m_Buttons[i].worldBound.Contains(pointerPos))
            {
                // 인덱스(0~8)를 h(-1,0,1)와 v(-1,0,1) 값으로 마법처럼 변환합니다!
                joyH = (i % 3) - 1; 
                joyV = 1 - (i / 3);
                return; // 찾았으니 함수 끝내기
            }
        }
        
        // 만약 패널 안이긴 한데 9개 버튼 영역 사이사이를 빗겨나갔다면 멈춤
        joyH = 0;
        joyV = 0;
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal") + joyH;
        float v = Input.GetAxisRaw("Vertical") + joyV;

        // 값이 -1~1 사이를 넘지 않도록 안전장치
        h = Mathf.Clamp(h, -1, 1);
        v = Mathf.Clamp(v, -1, 1);
        
        if ((isTouchRight && h == 1) || (isTouchLeft && h == -1))
        {
            h = 0;
        }

        if ((isTouchTop && v == 1) || (isTouchBottom && v == -1))
        {
            v = 0;
        }

        Vector3 curPos = transform.position;
        Vector3 nextPos = new Vector3(h, v, 0) * speed * Time.deltaTime;

        transform.position = curPos + nextPos;

        if (h != prevH)
        {
            anim.SetInteger("Input", (int)h);
            prevH = h;
        }
    }

    void Fire()
    {
        // if (!Input.GetButton("Fire1"))
        // {
        //     return;
        // }

        if (! isButtonAPressed)
        {
            return; 
        }
        
        if (curShotDelay < maxShotDelay)
        {
            return;
        }

        switch (power)
        {
            case 1:
                //기본 총알
                GameObject bullet = objectManager.MakeObj("bulletPlayerA");
                bullet.transform.position = transform.position;
                Rigidbody2D rigid = bullet.GetComponent<Rigidbody2D>();
                rigid.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
                break;
            case 2:
                GameObject bulletR = objectManager.MakeObj("bulletPlayerA");
                bulletR.transform.position = transform.position + Vector3.right * 0.1f;
                GameObject bulletL = objectManager.MakeObj("bulletPlayerA");
                bulletL.transform.position = transform.position + Vector3.left * 0.1f;
                Rigidbody2D rigidR = bulletR.GetComponent<Rigidbody2D>();
                Rigidbody2D rigidL = bulletL.GetComponent<Rigidbody2D>();
                rigidR.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
                rigidL.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
                break;
            default:
                GameObject bulletRR = objectManager.MakeObj("bulletPlayerA");
                bulletRR.transform.position = transform.position + Vector3.right * 0.25f;
                GameObject bulletCC = objectManager.MakeObj("bulletPlayerB");
                bulletCC.transform.position = transform.position;
                GameObject bulletLL = objectManager.MakeObj("bulletPlayerA");
                bulletLL.transform.position = transform.position + Vector3.left * 0.25f;

                Rigidbody2D rigidRR = bulletRR.GetComponent<Rigidbody2D>();
                Rigidbody2D rigidCC = bulletCC.GetComponent<Rigidbody2D>();
                Rigidbody2D rigidLL = bulletLL.GetComponent<Rigidbody2D>();
                rigidRR.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
                rigidCC.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
                rigidLL.AddForce(Vector2.up * 10, ForceMode2D.Impulse);
                break;
        }

        curShotDelay = 0;
    }

    void Reload()
    {
        curShotDelay += Time.deltaTime;
    }

    void Boom()
    {
        if (! isButtonBPressed)
        {
            return;
        }
        
        // if (!Input.GetButton("Fire2"))
        //     return;

        if (isBoomTime)
            return;

        if (boom == 0)
            return;

        boom--;
        isBoomTime = true;
        gameManager.UpdateBoomIcon(boom);

        boomEffect.SetActive(true);
        Invoke("OffBoomEffect", 2f);

        GameObject[] enemiesS = objectManager.GetPool("enemyS");
        GameObject[] enemiesM = objectManager.GetPool("enemyM");
        GameObject[] enemiesL = objectManager.GetPool("enemyL");
        GameObject[] enemiesB = objectManager.GetPool("enemyB");

        for (int i = 0; i < enemiesS.Length; i++)
        {
            if (enemiesS[i].activeSelf)
            {
                Enemy enemyLogic = enemiesS[i].GetComponent<Enemy>();
                enemyLogic.OnHit(1000);
            }
        }

        for (int i = 0; i < enemiesM.Length; i++)
        {
            if (enemiesM[i].activeSelf)
            {
                Enemy enemyLogic = enemiesM[i].GetComponent<Enemy>();
                enemyLogic.OnHit(1000);
            }
        }

        for (int i = 0; i < enemiesL.Length; i++)
        {
            if (enemiesL[i].activeSelf)
            {
                Enemy enemyLogic = enemiesL[i].GetComponent<Enemy>();
                enemyLogic.OnHit(1000);
            }
        }
        
        for (int i = 0; i < enemiesB.Length; i++)
        {
            if (enemiesB[i].activeSelf)
            {
                Enemy enemyLogic = enemiesB[i].GetComponent<Enemy>();
                enemyLogic.OnHit(1000);
            }
        }

        GameObject[] bulletA = objectManager.GetPool("bulletEnemyA");
        GameObject[] bulletB = objectManager.GetPool("bulletEnemyB");
        GameObject[] bulletBossA = objectManager.GetPool("bulletBossA");
        GameObject[] bulletBossB = objectManager.GetPool("bulletBossB");

        for (int i = 0; i < bulletA.Length; i++)
        {
            if (bulletA[i].activeSelf)
            {
                bulletA[i].SetActive(false);
            }
        }

        for (int i = 0; i < bulletB.Length; i++)
        {
            if (bulletB[i].activeSelf)
            {
                bulletB[i].SetActive(false);
            }
        }
        for (int i = 0; i < bulletBossA.Length; i++)
        {
            if (bulletBossA[i].activeSelf)
            {
                bulletBossA[i].SetActive(false);
            }
        }
        for (int i = 0; i < bulletBossB.Length; i++)
        {
            if (bulletBossB[i].activeSelf)
            {
                bulletBossB[i].SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Border")
        {
            switch (collision.gameObject.name)
            {
                case "Top":
                    isTouchTop = true;
                    break;
                case "Bottom":
                    isTouchBottom = true;
                    break;
                case "Right":
                    isTouchRight = true;
                    break;
                case "Left":
                    isTouchLeft = true;
                    break;
            }
        }
        else if (collision.gameObject.tag == "Enemy" || collision.gameObject.tag == "EnemyBullet")
        {
            if(isRespawnTime)
                return;
            
            if (isHit)
            {
                return;
            }

            isHit = true;
            life--;
            gameManager.UpdateLifeIcon(life);
            gameManager.CallExplosion(transform.position, "P");

            if (life <= 0)
            {
                gameManager.GameOver();
            }
            else
            {
                gameManager.RespawnPlayer();
            }

            gameObject.SetActive(false);
            collision.gameObject.SetActive(false);
        }
        else if (collision.gameObject.tag == "Item")
        {
            Item item = collision.gameObject.GetComponent<Item>();
            switch (item.type)
            {
                case "Coin":
                    score += 1000;
                    break;
                case "Power":
                    if (power >= maxPower)
                        score += 500;
                    else
                    {
                        power++;
                        AddFollower();
                    }

                    break;
                case "Boom":
                    if (boom >= maxBoom)
                        score += 500;
                    else
                    {
                        boom++;
                        gameManager.UpdateBoomIcon(boom);
                    }

                    break;
            }

            collision.gameObject.SetActive(false);
        }
    }

    void AddFollower()
    {
        if (power == 4)
            followers[0].SetActive(true);
        else if (power == 5)
            followers[1].SetActive(true);
        else if (power == 6)
            followers[2].SetActive(true);
    }

    void OffBoomEffect()
    {
        boomEffect.SetActive(false);
        isBoomTime = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Border")
        {
            switch (collision.gameObject.name)
            {
                case "Top":
                    isTouchTop = false;
                    break;
                case "Bottom":
                    isTouchBottom = false;
                    break;
                case "Right":
                    isTouchRight = false;
                    break;
                case "Left":
                    isTouchLeft = false;
                    break;
            }
        }
    }
}