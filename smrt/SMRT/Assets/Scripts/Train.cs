﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Train : MonoBehaviour {

    public string m_RouteTag;
    public float m_TrainSpeed = 1.0f;
    public float m_TimeToWaitInStation = 0.0f;
    public int m_CurrentStationIndex = 0;
    public bool m_IncreaseToNextStation = true;
    public AudioSource m_SuccessSound;

    public Spawner      m_LineSpawner;
    private GameObject  m_RouteMaster;
    private RouteScript m_RouteComp;
    private Renderer    m_Renderer;
    private float m_DistanceTravelled;
    private float m_TimeInStation;
    private bool m_Collidied;
    private Collider m_CollidiedWith;
    private SMRTGameManager m_GameManager;
    private Vector4 m_LookAtCache;
    // Use this for initialization
    void Start () {
        m_RouteMaster = GameObject.FindGameObjectWithTag(m_RouteTag);
        m_GameManager = GameObject.FindGameObjectWithTag("GameController").GetComponent<SMRTGameManager>();
        m_RouteComp = m_RouteMaster.GetComponent<RouteScript>();
        m_Renderer = gameObject.GetComponent<Renderer>();

        m_DistanceTravelled = 0.0f;
        m_TimeInStation = 0.0f;
        m_CurrentStationIndex = 0;
        m_IncreaseToNextStation = true;
        m_Collidied = false;
        m_LookAtCache = GetGoToVector().normalized;
        UpdateTrain(GetCurrentStation(), GetGoToVector());
    }
    
    // Update is called once per frame
    void Update () {
        if(     m_GameManager
            && (!m_GameManager.m_LevelStarted || m_GameManager.m_IsGameOver))
        {
            return;
        }

        if (!m_GameManager.m_StartingTimer.IsElapsed())
        {
            GameObject.Destroy(gameObject);
        }
        
        Vector4 goToVec        = GetGoToVector();

        float totalDistance = goToVec.magnitude;
        // go to next station
        if (m_DistanceTravelled < totalDistance)
        {
            if (!m_Collidied)
            {
                m_DistanceTravelled += Time.deltaTime * m_TrainSpeed;
                if (m_DistanceTravelled >= totalDistance)
                {
                    m_DistanceTravelled = totalDistance;
                }

                UpdateTrain(GetCurrentStation() + (goToVec.normalized * m_DistanceTravelled), goToVec);
            }
            else
            {
                m_GameManager.IncrementHappiness(m_GameManager.m_HappinessForActiveCollision * Time.deltaTime);
            }
        }
        // wait at station
        else
        {
            m_TimeInStation += Time.deltaTime;
            if (   m_TimeInStation >= m_TimeToWaitInStation
                || DonNeedToWaitAtStation())
            {
                m_DistanceTravelled = 0.0f;
                m_TimeInStation = 0.0f;
                // Reach station increase happiness
                if (!m_GameManager.m_DaysTimer.IsElapsed())
                {
                    m_GameManager.IncrementHappiness(m_GameManager.m_HappinessForCompletingAStation);
                    m_GameManager.IncrementMoney(m_GameManager.m_MoneyGainWhenReachStation);
                }

                if (m_IncreaseToNextStation)
                {
                    if (m_CurrentStationIndex + 2 >= m_RouteComp.m_WayPoint.Length)
                    {
                        m_CurrentStationIndex = m_RouteComp.m_WayPoint.Length - 1;
                        m_IncreaseToNextStation = false;

                        if(    m_GameManager.m_IsGameOver
                            || m_GameManager.m_DaysTimer.IsElapsed())
                        {
                            GameObject.Destroy(gameObject);
                            m_LineSpawner.m_SpawnedTrain -= 1;
                        }
                    }
                    else
                    {
                        ++m_CurrentStationIndex;
                    }
                }
                else
                {
                    if (m_CurrentStationIndex <= 1)
                    {
                        m_CurrentStationIndex = 0;
                        m_IncreaseToNextStation = true;

                        if(    m_GameManager.m_IsGameOver
                            || m_GameManager.m_DaysTimer.IsElapsed())
                        {
                            GameObject.Destroy(gameObject);
                            m_LineSpawner.m_SpawnedTrain -= 1;
                        }
                    }
                    else
                    {
                        --m_CurrentStationIndex;
                    }
                }
                // move to next station
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        //Renderer render = GetComponent<Renderer>();
        //render.material.color

        Train otherTrain = other.GetComponent<Train>();
        
        if (   otherTrain
            && otherTrain.m_RouteTag == m_RouteTag)
        {
            //int nextStationIndex = GetNextStationIndex();
            if (otherTrain.m_CurrentStationIndex == m_CurrentStationIndex
                && m_DistanceTravelled < GetGoToVector().magnitude
                && !m_Collidied)
            {
                m_Renderer.material.color = new Color(1.0f, 0.0f, 0.0f);
                m_Collidied = true;
                m_CollidiedWith = other;
                // Langa
                m_GameManager.IncrementHappiness(m_GameManager.m_HappinessForTrainSlowdown);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other == m_CollidiedWith)
        {
            m_Collidied = false;
            m_Renderer.material.color = new Color(1.0f, 1.0f, 1.0f);
        }
    }

    void OnMouseDown()
    {
        GameObject.Destroy(gameObject);
        m_LineSpawner.m_SpawnedTrain -= 1;

        // if not break down,
        m_GameManager.IncrementMoney(m_GameManager.m_MoneyLostForPullingATrainOutOfService);

        if(m_SuccessSound)
        {
            m_SuccessSound.Play();
        }
    }

    private int GetNextStationIndex()
    {
        return m_IncreaseToNextStation ? m_CurrentStationIndex + 1 : m_CurrentStationIndex - 1;
    }

    private Vector4 GetCurrentStation()
    {
        return m_RouteComp.m_WayPoint[m_CurrentStationIndex].transform.position;
    }

    private bool DonNeedToWaitAtStation()
    {
        bool forcedGo = m_GameManager.m_IsGameOver || m_GameManager.m_DaysTimer.IsElapsed();
        if (forcedGo)
        {
            return true;
        }
        return m_RouteComp.m_WayPoint[GetNextStationIndex()].tag == "NotAStation";
    }
    private Vector4 GetNextStation()
    {
        return m_RouteComp.m_WayPoint[GetNextStationIndex()].transform.position;
    }
    private Vector4 GetGoToVector()
    {
        
        Vector4 currentStation = GetCurrentStation();
        Vector4 nextStation    = GetNextStation();
        return nextStation - currentStation;
    }

    private void UpdateTrain(Vector4 position, Vector4 lookAt)
    {
        float blendWeight = 0.2f;
        float stayWeight = 1.0f - blendWeight;
        m_LookAtCache = (m_LookAtCache * stayWeight) + (lookAt.normalized * blendWeight);
        Vector3 upVec = new Vector3(0.0f, 0.0f, 1.0f);
        Vector3 rightVec = Vector3.Cross(upVec, lookAt.normalized) *0.02f;
        Vector3 v3Position = position;
        gameObject.transform.position = (gameObject.transform.position * stayWeight) + ((v3Position + rightVec) * blendWeight);
        gameObject.transform.rotation = Quaternion.LookRotation(m_LookAtCache); ;
    }
}
