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
    public string[] enemyObjs;
    public Transform[] spawnPoints;
    
    public float nextSpawnDelay;
    public float curSpawnDelay;

    public GameObject player;
    public ObjectManager objectManager;
    
    private Label m_Score;
    private List<VisualElement> m_Lifes;
    private List<VisualElement> m_Booms;
    private VisualElement m_GameOver;
    private Button m_restartButton;

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
        
        if (m_restartButton != null)
        {
            m_restartButton.clicked += GameRetry;
        }
        
        spawnList = new List<Spawn>();
        enemyObjs = new string[] { "enemyS", "enemyM", "enemyL" };
        ReadSpawnFile();
    }

    void ReadSpawnFile()
    {
        spawnList.Clear();
        spawnIndex = 0;
        spawnEnd = false;
        
        TextAsset textFile = Resources.Load("Stage 0") as TextAsset;
        StringReader stringReader = new StringReader(textFile.text);

        while (stringReader != null)
        {
            string line = stringReader.ReadLine();
            Debug.Log(line);
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
        }
        int enemyPoint = spawnList[spawnIndex].point;
        GameObject enemy = objectManager.MakeObj(enemyObjs[enemyIndex]);
        enemy.transform.position = spawnPoints[enemyPoint].position;
        
        Rigidbody2D rigid = enemy.GetComponent<Rigidbody2D>();
        Enemy enemyLogic = enemy.GetComponent<Enemy>();
        enemyLogic.player = player;
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

    public void GameOver()
    {
        m_GameOver.style.display = DisplayStyle.Flex;
    }

    public void GameRetry()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
