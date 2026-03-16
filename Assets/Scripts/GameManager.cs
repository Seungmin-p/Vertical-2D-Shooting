using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;
using System.IO;

public class GameManager : MonoBehaviour
{
    public int stage;
    
    public string[] enemyObjs;
    public Transform[] spawnPoints;
    public Animator fadeAnim;
    public Transform playerPos;
    
    public float nextSpawnDelay;
    public float curSpawnDelay;

    public GameObject player;
    public ObjectManager objectManager;
    
    private Label m_Score;
    private List<VisualElement> m_Lifes;
    private List<VisualElement> m_Booms;
    private VisualElement m_GameOver;
    private Button m_restartButton;
    
    private Label m_StageText;

    public List<Spawn> spawnList;
    public int spawnIndex;
    public bool spawnEnd;
    
    void Awake()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;
        
        m_Score = root.Q<Label>("Score");
        var lifeContainer = root.Q<VisualElement>("LifeContainer");
        m_Lifes = lifeContainer.Query<VisualElement>("Life").ToList();
        var boomContainer = root.Q<VisualElement>("BoomContainer");
        m_Booms = boomContainer.Query<VisualElement>("Boom").ToList();
        m_GameOver = root.Q<VisualElement>("GameOver");
        m_restartButton = root.Q<Button>("RestartButton");
        m_StageText = root.Q<Label>("StageText");
        
        if (m_restartButton != null)
        {
            m_restartButton.clicked += GameRetry;
        }
        
        spawnList = new List<Spawn>();
        enemyObjs = new string[] { "enemyS", "enemyM", "enemyL", "enemyB" };
        StageStart();
    }

    public void StageStart()
    {
        Invoke("PrintStartText", 0.1f);
        ReadSpawnFile();
        
        fadeAnim.SetTrigger("In");
    }

    void PrintStartText()
    {
        PlayStageText("Stage " + stage + "\nStart!");
    }
    
    void PrintClearText()
    {
        PlayStageText("Stage " + stage + "\nClear!");
    }

    public void StageEnd()
    {
        PrintClearText();
        
        fadeAnim.SetTrigger("Out");
        
        player.transform.position = playerPos.position;
        
        stage++;

        if (stage > 2)
        {
            Invoke("GameOver",5f);
        }
        else
        {
            Invoke("StageStart",5f);
        }
        
    }
    
    public void PlayStageText(string message)
    {
        if (m_StageText == null) return;
        
        m_StageText.text = message;
        
        m_StageText.style.opacity = 1f; 
        m_StageText.style.scale = new StyleScale(new Vector2(1f, 1f));
        
        Invoke("HideStageText", 2f);
    }

    private void HideStageText()
    {
        // 투명도는 0으로 스르륵 흐려지게 둡니다.
        m_StageText.style.opacity = 0f; 
    
        // 💡 핵심: 0.5배가 아니라 아예 0배(점)로 확 쪼그라들게 만듭니다!
        m_StageText.style.scale = new StyleScale(new Vector2(0f, 0f));
    }
    
    void ReadSpawnFile()
    {
        spawnList.Clear();
        spawnIndex = 0;
        spawnEnd = false;
        
        TextAsset textFile = Resources.Load("Stage " + stage) as TextAsset;
        StringReader stringReader = new StringReader(textFile.text);

        while (stringReader != null)
        {
            string line = stringReader.ReadLine();
            // Debug.Log(line);
            if(line == null)
                break;
            
            Spawn spawnData = new Spawn();
            spawnData.delay = float.Parse(line.Split(',')[0]);
            spawnData.type = line.Split(',')[1];
            spawnData.point = int.Parse(line.Split(',')[2]);
            spawnList.Add(spawnData);
        }
        
        stringReader.Close();
        nextSpawnDelay = spawnList[0].delay;
    }
    
    void Update()
    {
        curSpawnDelay += Time.deltaTime;

        if (curSpawnDelay > nextSpawnDelay && ! spawnEnd)
        {
            SpawnEnemy();
            curSpawnDelay = 0;
        }
        
        Player playerLogic = player.GetComponent<Player>();
        m_Score.text = string.Format("{0:n0}", playerLogic.score);
    }

    void SpawnEnemy()
    {
        int enemyIndex = 0;
        switch (spawnList[spawnIndex].type)
        {
            case "S" :
                enemyIndex = 0;
                break;
            case "M" :
                enemyIndex = 1;
                break;
            case "L" :
                enemyIndex = 2;
                break;
            case "B" :
                enemyIndex = 3;
                break;
        }
        int enemyPoint = spawnList[spawnIndex].point;
        GameObject enemy = objectManager.MakeObj(enemyObjs[enemyIndex]);
        enemy.transform.position = spawnPoints[enemyPoint].position;
        
        Rigidbody2D rigid = enemy.GetComponent<Rigidbody2D>();
        Enemy enemyLogic = enemy.GetComponent<Enemy>();
        enemyLogic.player = player;
        enemyLogic.gameManager = this;
        enemyLogic.objectManager = objectManager;

        if (enemyPoint == 5 || enemyPoint == 6)
        {
            enemy.transform.Rotate(Vector3.back * 90);
            rigid.linearVelocity = new Vector2(enemyLogic.speed*(-1), -1);
        }
        else if (enemyPoint == 7 || enemyPoint == 8)
        {
            enemy.transform.Rotate(Vector3.forward * 90);
            rigid.linearVelocity = new Vector2(enemyLogic.speed, -1);
        }
        else
        {
            rigid.linearVelocity = new Vector2(0, enemyLogic.speed*(-1));
        }
        
        spawnIndex++;
        if (spawnIndex == spawnList.Count)
        {
            spawnEnd = true;
            return;
        }
        
        nextSpawnDelay = spawnList[spawnIndex].delay;
    }

    public void UpdateLifeIcon(int life)
    {
        for (int i = 0; i < 3; i++)
        {
            m_Lifes[i].style.display = DisplayStyle.None;
        }
        
        for (int i = 0; i < life; i++)
        {
            m_Lifes[i].style.display = DisplayStyle.Flex;
        }
    }
    
    public void UpdateBoomIcon(int boom)
    {
        for (int i = 0; i < 3; i++)
        {
            m_Booms[i].style.display = DisplayStyle.None;
        }
        
        for (int i = 0; i < boom; i++)
        {
            m_Booms[i].style.display = DisplayStyle.Flex;
        }
    }

    public void RespawnPlayer()
    {
        Invoke("RespawnPlayerExe",2f);
    }
    void RespawnPlayerExe()
    {
        player.transform.position = Vector3.down * 4.0f;
        player.SetActive(true);
        
        Player playerLogic = player.GetComponent<Player>();
        playerLogic.isHit = false;
    }

    public void CallExplosion(Vector3 pos, string type)
    {
        GameObject explosion = objectManager.MakeObj("explosion");
        Explosion explosionLogic = explosion.GetComponent<Explosion>();
        
        explosion.transform.position = pos;
        explosionLogic.StartExplosion(type);
    }
    
    public void GameOver()
    {
        m_GameOver.style.display = DisplayStyle.Flex;
    }

    public void GameRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
