using UnityEngine;

public class Background : MonoBehaviour
{
    public float speed;
    public int startIndex;
    public int endIndex;
    public Transform[] sprites;
    
    float viewHeight;

    private void Awake()
    {
        viewHeight = Camera.main.orthographicSize * 2;
    }
    
    void Update()
    {
        Move();
        Scrolling();
    }

    void Move()
    {
        Vector3 curPos = transform.position;
        Vector3 nextPos = Vector3.down * speed * Time.deltaTime;
        transform.position = curPos + nextPos;
    }

    void Scrolling()
    {
        if (sprites[endIndex].position.y < viewHeight*(-1))
        {
            Vector3 backSpriteLocalPos = sprites[startIndex].localPosition;
            
            // 맨 아래에 있던 녀석을 맨 위에 있는 녀석 바로 위로 순간이동
            sprites[endIndex].localPosition = backSpriteLocalPos + Vector3.up * viewHeight;
            
            // [수정 2] 다음 타자를 위한 인덱스 정리 (endIndex를 기준으로 계산)
            int endIndexSave = endIndex; // 방금 위로 텔레포트한 녀석
            startIndex = endIndexSave;   // 걔가 이제 새로운 맨 위(Start)가 됨
            
            // 끝 번호는 방금 올라간 녀석의 앞 번호로 지정 (-1 하되 0보다 작아지면 배열 끝으로)
            endIndex = (endIndexSave - 1 == -1) ? sprites.Length - 1 : endIndexSave - 1;
        }
    }
}
