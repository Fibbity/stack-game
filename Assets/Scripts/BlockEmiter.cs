﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockEmiter : MonoBehaviour {
    [SerializeField]
    Vector3 emitDirection;
	// Use this for initialization

	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void CreateBlock()
    {

    }
    public Vector3 EmitDirection { get {return emitDirection; }}


}
