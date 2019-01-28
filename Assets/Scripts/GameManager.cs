﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Init();
    }


    ///GetComponent<GameManager>();
    public GameManager()
    {
        Debug.Log("GameManager.Constructor");


    }
    [SerializeField]
    Block sampleBlock;

    [SerializeField]
    GameConfig config;



    [SerializeField]
    BlockEmiter leftEmiter;
    [SerializeField]
    BlockEmiter rightEmiter;
    [SerializeField]
    GameObject ground;

    List<Plane> blockWallPlane;
    [SerializeField]
    Text scoreText;
    [SerializeField]
    Vector3 fallingPoint;


    GameState gameState;
    GamePlayState playState;
    float timeStartPlay;
    float timeLastEmit;

    Block topBlock;
    List<Block> blockStack;
    List<Block> blockStopped;
    Block flyingBlock;
    int blockCounter;
    float currentHeight = 0;
    Vector3 currentRoot = Vector3.zero;
    int score;
    BlockPool blockPool;

    public void Init()
    {
        this.config = GameConfig.CreateDefaultGameConfig();
        this.blockPool = new BlockPool(this.sampleBlock, 100);

        this.gameState = GameState.Play;
        this.playState = GamePlayState.Prepare;
        this.blockStack = new List<Block>();
        this.blockStopped = new List<Block>();
        this.currentHeight = 0;
        this.topBlock = null;
        this.blockWallPlane = new List<Plane>();

        foreach(GameObject blockWall in GameObject.FindGameObjectsWithTag("BlockWall"))
        {
            MeshFilter mesh = blockWall.GetComponent<MeshFilter>();
            Plane p = new Plane(blockWall.transform.TransformPoint(mesh.mesh.vertices[0])
                , blockWall.transform.TransformPoint(mesh.mesh.normals[1])
                , blockWall.transform.TransformPoint(mesh.mesh.normals[2])
                );
            Debug.Log(string.Format("WallCreated: {0}, {1}, {2}"
                , blockWall.transform.TransformPoint(mesh.mesh.vertices[0])
                , blockWall.transform.TransformPoint(mesh.mesh.vertices[1])
                , blockWall.transform.TransformPoint(mesh.mesh.vertices[2])));
            this.blockWallPlane.Add(p);
        }
        Debug.Log("Planes: " + this.blockWallPlane.Count);

        StartPlayGame();
    }

    public void StartPlayGame()
    {
        Debug.Log(string.Format("Ground size: {0}, {1}", ground.transform.localScale.x, ground.transform.localScale.z));
        this.playState = GamePlayState.Prepare;
        
        ///Reset playing state
        this.timeStartPlay = Time.time;
        this.blockStack.Clear();
        this.blockStopped.Clear();
        this.topBlock = null;
        this.blockCounter = 0;
        this.score = 0;
        
        ///Setup first block
        Block genesisBlock = blockPool.GetBlock();
        genesisBlock.SetActive();

        Vector3 pos = this.fallingPoint;
        pos.y = this.fallingPoint.y + genesisBlock.transform.localScale.y/2;
        genesisBlock.transform.position = pos;

        AddNewBlockToStack(genesisBlock);
    }

    // Use this for initialization
    void Start () {
        GameObject obj = GameObject.Find("Block-Ex1");
        
	}
	
	// Update is called once per frame
	void FixedUpdate () {
        if (this.gameState == GameState.Play)
        {
            if (playState == GamePlayState.Playing)
            {
                if (this.flyingBlock != null)
                {
                    ///Flying out of range: fall
                    float distance = Vector3.Distance(this.flyingBlock.transform.position, currentRoot);
                    ///Debug.Log("Flying block distance: " + distance);
                    if (distance > config.MaxBlockTravelDistance)
                    {
                        this.flyingBlock.StopFlying();
                    }

                    ///Falling to ground: die
                    if (this.flyingBlock.transform.position.y < this.fallingPoint.y)
                    {
                        HandleBlockOnGround(this.flyingBlock);
                        this.flyingBlock = null;
                    }
                }
            }
        }
    }
    private void Update()
    {
        if (this.gameState == GameState.Play)
        {
            UpdatePlaying();
        }
    }
    void StartGamePlayingState()
    {
        this.playState = GamePlayState.Playing;
        this.timeLastEmit = 0;
        this.flyingBlock = null;
    }
    


    void EmitBlock()
    {
        UpdateEmiterPosition();

        ///Setup new block
        blockCounter++;
        Block newBlock = blockPool.GetBlock();
        
        newBlock.name = "flying-block-" + blockCounter;
        Rigidbody blockBody = newBlock.GetComponent<Rigidbody>();
        blockBody.mass = 0.1f;
        blockBody.useGravity = false;
        blockBody.isKinematic = false;
        newBlock.SetActive();

        ///a little higher than emiter
        Vector3 newPos = this.leftEmiter.transform.position;
        newPos.y += newBlock.transform.localScale.y/2;
        newBlock.transform.position = newPos;
        Debug.Log(string.Format("EmitBlock: emitter={0}, newBlock={1}", newBlock.transform.position.ToString(), newBlock.transform.position));

        ///Track on ground
        newBlock.OnGroundListener.AddListener(HandleBlockOnGround);
        newBlock.OnBlockListener.AddListener(HandleBlockOnBlock);

        ///Fly bro
        blockBody.AddForce(this.leftEmiter.EmitDirection * config.EmitForce, ForceMode.Impulse);
        newBlock.StartFlying();
        ///Record
        this.timeLastEmit = Time.time;
        this.flyingBlock = newBlock;
    }

    float newHeight = 0;
    void UpdateEmiterPosition()
    {
        this.newHeight = 0;
        foreach (Block block in this.blockStack)
        {
            this.newHeight += block.transform.localScale.y;
        }
        this.currentHeight = newHeight;
        this.currentRoot = this.fallingPoint;
        this.currentRoot.y = this.currentHeight;

        Vector3 newPos = this.leftEmiter.transform.position;
        newPos.y = this.newHeight;
        this.leftEmiter.transform.position = newPos;
        ///Debug.Log(string.Format("UpdateEmiterPosition: stack={0}, height={1}, emiter={2}", this.blockStack.Count, currentHeight, leftEmiter.transform.position));
        
    }

    void UpdatePlaying()
    {
        if (this.playState == GamePlayState.Prepare)
        {
            if (Time.time - timeStartPlay > config.StatePlayingPrepareSecond)
            {
                Debug.Log("UpdatePlaying: Prepare done, go to Playing!");
                StartGamePlayingState();

            }
        }
        else if (this.playState == GamePlayState.Playing)
        {
            if (Time.time - this.timeLastEmit > config.BlockEmitIntervalSecond && this.flyingBlock == null)
            {
                EmitBlock();
                UpdateEmiterPosition();
            }

            ///User stop flying block
            if (Input.GetAxis("Jump") == 1 && this.flyingBlock != null)
            {
                Debug.Log("InputAxis: " + Input.GetAxis("Jump"));
                Debug.Log("Stop!!!");
                this.flyingBlock.StopFlying();
                blockStopped.Add(this.flyingBlock);
                this.flyingBlock = null;
            }

            
        }
    }

    private void LateUpdate()
    {
        foreach (Block block in this.blockStopped)
        {
            Rigidbody body = block.GetComponent<Rigidbody>();
            block.OnGroundListener.AddListener(HandleBlockOnGround);
            ///body.isKinematic = true;
            Debug.Log(string.Format("LateUpdate: {0}", body.position));
        }
        blockStopped.Clear();

        this.scoreText.text = "Score: " + score;
    }
    
    public void AddNewBlockToStack(Block newBlock)
    {
        newBlock.SetOnStack();
        newBlock.OnBlockListener.RemoveAllListeners();
        this.blockStack.Add(newBlock);
        this.topBlock = newBlock;
        Debug.Log(string.Format("AddNewBlockToStack: {0}, flying={1}, onStack={2}, top={3}", newBlock.name, newBlock.IsFlying, newBlock.IsOnStack, topBlock));
        ///Calculate emitters position
        UpdateEmiterPosition();
    }

    void EndGame()
    {
        this.playState = GamePlayState.Finish;
        Debug.Log(string.Format("GameEnd: {0}", score));
    }
    public void HandleBlockOnGround(Block target)
    {
        Debug.Log("HandleBlockOnGround: " + target.transform.gameObject.name);
        EndGame();
    }
    public void HandleBlockDisappear(Block block)
    {
        Destroy(block);
    }
    public void HandleBlockOnBlock(Block actor, Block target, Collision collision)
    {
        Debug.Log(string.Format("HandleBlockOnBlock: {0} on {1}"
            , actor.transform.gameObject.name
            , target.transform.gameObject.name));

        if (target == topBlock)
        {
            score += 1;
            AddNewBlockToStack(actor);

            TrimBlockToCollision(actor, target, collision);
        }
        else {
            Debug.Log(string.Format("HandleBlockOnBlock: {0} on {1}, top block is {2}, go to hell"
                        , actor.transform.gameObject.name
                        , target.transform.gameObject.name
                        , topBlock.transform.gameObject.name));
        }
    }
    public void TrimBlockToCollision(Block target, Block pattern, Collision collision)
    {
        Debug.Log(string.Format("TrimBlockToCollision: {0}, {1}", target.name, pattern.name));
        UtilCube.TrimBlockToCollision(target, pattern, collision);
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(this.fallingPoint, new Vector3(1, 1, 1));
    }

}
