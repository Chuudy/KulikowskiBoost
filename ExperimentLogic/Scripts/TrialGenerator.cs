﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialGenerator : MonoBehaviour
{
    [Header("Prefabs and materials")]
    public GameObject player;
    public GameObject instancedObject;
    public GameObject[] instancedObjects;
    public GameObject feedBackObjectCorrect;
    public GameObject feedBackObjectInCorrect;
    private GameObject feedBackObjectInstance;
    public Material SkyBoxMaterial;
    private Material SkyBoxMaterialTmp;
    public Material StimuliMaterial;

    [Header("Stimuli transform options")]
    public Vector2 distanceRange = new Vector2(5f, 10f);
    public Vector2 scaleRange = new Vector2(0.8f,1.2f);
    public float angleBetweenCenters = 30;
    public float baseDistance;
    public float secondObjectDistance;
    [Range(0.001f, 0.2f)]
    public float dioptricDistnaceDifference = 0.1f;

    [Header("Stimuli contrast options")]
    [Range(0f, 1f)]
    public float backgroundValue = 0.5f;
    [Range(0f, 0.5f)]
    public float contrast = 0.1f;

    [Header("Modes")]
    public bool perceptualyEqualizeSize = false;
    public bool removeParalax = false;

    [Header("Experiment settings")]
    public string resultsFilename = "results.csv";
    public string participantId = "PutIdHere";
    public int blankScreenTime = 1;
    public int reversalsToTerminate = 20;
    public float distanceStep = 0.005f;


    private List<GameObject> instances;
    private Vector3 initialPlayerDirection;
    private Vector3 initialPlayerPosition;

    private int closerIndex;

    private int currentCorrectAnswersNumber=0;
    private int currentReversalsNumber = 0;
    private bool lastAnswer;
    private float timer;

    private bool inputBlocked = false;

    // Start is called before the first frame update
    void Start()
    {
        ShowFeedback(true);
        CreateResultsFile();

        SkyBoxMaterialTmp = new Material(SkyBoxMaterial);
        RenderSettings.skybox = SkyBoxMaterialTmp;

        instances = new List<GameObject>();

        NewTrial();
    }

    // Update is called once per frame
    void Update()
    {
        if(!inputBlocked)
        {
            KeyboardInput();
            ChangeContrastParameters();
            if (perceptualyEqualizeSize)
                instances[1].transform.localScale = instances[0].transform.localScale * (secondObjectDistance / baseDistance);
        }

        timer += Time.deltaTime;

        //secondObjectDistance = 1 / (1f / baseDistance - dioptricDistnaceDifference);

        //Vector3 tmpDirecrtion;

        //if (removeParalax)
        //    tmpDirecrtion = player.transform.forward;
        //else
        //    tmpDirecrtion = initialPlayerDirection;

        //instances[0].transform.position = initialPlayerPosition + Quaternion.AngleAxis(-angleBetweenCenters/2, Vector3.up) * tmpDirecrtion * baseDistance;
        //secondObjectDistance = 1 / (1f / baseDistance - dioptricDistnaceDifference);
        //instances[1].transform.position = initialPlayerPosition + Quaternion.AngleAxis(angleBetweenCenters/2, Vector3.up) * tmpDirecrtion * secondObjectDistance;

       

    }

    void CreateInstances()
    {
        Vector3 position;
        Quaternion rotation;
        GameObject instance;
        LookAtConstraint constraint;

        baseDistance = Random.Range(distanceRange.x, distanceRange.y);
        position = initialPlayerPosition + initialPlayerDirection * baseDistance;
        rotation = Quaternion.identity;

        int randomIndex = Random.Range(0, instancedObjects.Length);
        instance = Instantiate(instancedObjects[randomIndex], position, rotation);
        instance.transform.RotateAroundLocal(new Vector3(0, 0, 1), Random.RandomRange(0, 360));
        constraint = instance.GetComponent(typeof(LookAtConstraint)) as LookAtConstraint;
        constraint.lookAtObject = player.transform;
        instances.Add(instance);        

        secondObjectDistance = 1 / (1f / baseDistance + dioptricDistnaceDifference);
        position = initialPlayerPosition + initialPlayerDirection * secondObjectDistance;
        rotation = Quaternion.identity;

        randomIndex = Random.Range(0, instancedObjects.Length);
        instance = Instantiate(instancedObjects[randomIndex], position, rotation);
        constraint = instance.GetComponent(typeof(LookAtConstraint)) as LookAtConstraint;
        constraint.lookAtObject = player.transform;
        instances.Add(instance);

    }

    void ChangeContrastParameters()
    {

        SkyBoxMaterialTmp.SetFloat("_BaseValue", backgroundValue);
        StimuliMaterial.SetFloat("_BaseValue", backgroundValue);
        StimuliMaterial.SetFloat("_Contrast", contrast);        
    }

    void NewTrial()
    {
        CreateInstances();

        closerIndex = (int)Mathf.Round(Random.value);
        float closerAngleFactor = closerIndex * 2 - 1;

        Debug.Log(closerAngleFactor);

        initialPlayerDirection = player.transform.forward;
        initialPlayerPosition = player.transform.position;

        instances[0].transform.position = initialPlayerPosition + Quaternion.AngleAxis(closerAngleFactor * angleBetweenCenters / 2, player.transform.up) * initialPlayerDirection * baseDistance;
        
        secondObjectDistance = 1 / (1f / baseDistance - dioptricDistnaceDifference);

        instances[1].transform.position = initialPlayerPosition + Quaternion.AngleAxis(-closerAngleFactor * angleBetweenCenters / 2, player.transform.up) * initialPlayerDirection * secondObjectDistance;

        instances[0].transform.SetParent(player.transform, true);
        instances[1].transform.SetParent(player.transform, true);

        Debug.Log("New Trial created");

        inputBlocked = false;
        timer = 0;

        //DeactivateLookAtConstraints();
    }

    void DeleteExistingInstnaces()
    {
        foreach (GameObject instance in instances)
        {
            GameObject.Destroy(instance);
        }

        instances.Clear();
    }

    void DeactivateLookAtConstraints()
    {
        LookAtConstraint constraint;

        foreach (GameObject instance in instances)
        {
            GameObject.Destroy(instance);
            constraint = instance.GetComponent(typeof(LookAtConstraint)) as LookAtConstraint;
            constraint.enabled = false;
        }
    }

    void CheckAnswer(int answer)
    {
        bool currentAnswer = (answer == closerIndex);

        if(currentAnswer)
        {
            Debug.Log("Correct");
            currentCorrectAnswersNumber++;
            if(currentCorrectAnswersNumber%2 == 0)
                ChangeDistance(-1);
        }
        else
        {
            Debug.Log("Incorrect");
            currentCorrectAnswersNumber = 0;
            ChangeDistance(1);
        }

        if (currentAnswer != lastAnswer)
            currentReversalsNumber++;

        AddRecord(currentAnswer);
        lastAnswer = currentAnswer;

        if(currentReversalsNumber >= reversalsToTerminate)
        {
            Debug.Log("Quit");
            QuitExperiment();
        }
        
        DeleteExistingInstnaces();
        inputBlocked = true;
        ShowFeedback(currentAnswer);
        Invoke("NewTrial", blankScreenTime);
    }

    void ChangeDistance(int direction)
    {
        dioptricDistnaceDifference = Mathf.Max((float)System.Math.Round(System.Convert.ToDouble(dioptricDistnaceDifference + direction * distanceStep),3),0.001f);
    }

    void QuitExperiment()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    void KeyboardInput()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            NewTrial();
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            CheckAnswer(0);
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            CheckAnswer(1);
        }
    }

    void ShowFeedback(bool answer)
    {
        if (answer)
            feedBackObjectCorrect.active = true;
        else
            feedBackObjectInCorrect.active = true;


        Invoke("RemoveFeedback", 0.75f);
    }

    void RemoveFeedback()
    {
        feedBackObjectCorrect.active = false;
        feedBackObjectInCorrect.active = false;
    }

    void CreateResultsFile()
    {
        if (!System.IO.File.Exists(resultsFilename))
        {
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(resultsFilename, false))
            {
                file.WriteLine("id,condition,distance,answer,time");
            }
        }
    }
    
    public void AddRecord(bool answer)
    {
        using (System.IO.StreamWriter file = new System.IO.StreamWriter(resultsFilename, true))
        {
            file.WriteLine(participantId + "," + "0" + "," + dioptricDistnaceDifference + "," + answer.ToString() + "," + timer.ToString());
        }
    }

}
