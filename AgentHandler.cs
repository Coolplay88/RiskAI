using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class AgentHandler : MonoBehaviour
{
    public BoardController bc;
    public Genome agent;
    public Population_Manager manager;
    private Knoten[] maskGroup1;
    private Knoten[] maskGroup2;

    // Add tracking variables
    public float missionProgress = 0f;
    public bool hasWon = false;
    private int totalMissions = 0;
    private int completedMissions = 0;

    public void Start()
    {
        manager = Camera.main.GetComponent<Population_Manager>();

        // Initialize mask groups
        maskGroup1 = new Knoten[42];
        maskGroup2 = new Knoten[2];

        if (agent != null)
        {
            var outputNodes = agent.Kn.Where(k => k.type == 2).ToList();

            for (int i = 0; i < maskGroup1.Length; i++)
            {
                maskGroup1[i] = outputNodes[i];
            }
            maskGroup2[0] = outputNodes[outputNodes.Count - 2];
            maskGroup2[1] = outputNodes[outputNodes.Count - 1];
        }

        // Subscribe to mission events
        if (bc != null)
        {
            bc.OnMissionComplete += HandleMissionComplete;
            bc.OnGameEnd += HandleGameEnd;
        }
    }

    private void HandleMissionComplete()
    {
        completedMissions++;
        totalMissions++;
        UpdateMissionProgress();
    }

    private void HandleGameEnd()
    {
        hasWon = bc.winner == gameObject;
        UpdateMissionProgress();
    }

    private void UpdateMissionProgress()
    {
        missionProgress = totalMissions > 0 ? (float)completedMissions / totalMissions : 0f;
    }

    public int NNPredict(List<float> input, int actionIndex,int Player_id)
    {
        //Action index 0,1,3,6,7
        if (actionIndex == 0 || actionIndex==1||actionIndex==3||actionIndex==6||actionIndex==7)
        {
            if (actionIndex == 0)
            {
                //Überprüfen ob überhaupt 2 Würfel gewählt werden kann
                if (bc.boardState[(int)input[92]][1] >= 2)
                {
                    ApplyMask(2);



                }
                else
                {
                    //OutputNeuron für Aktion überspringen
                    return 43;
                }
            }
            //Boni einlösen (wurde schon geprüft) nur noch maskieren
            else if (actionIndex == 1)
            {
                ApplyMask(2);
            }
            //Angreifen J/N
            else if (actionIndex == 3)
            {
                var hasAttackT = 0;

                //Überprüfe ob es ein potenzielles Angreifer Land gibt
                for(int i = 0; i < bc.playersTerritories[Player_id].Count; i++)
                {
                    //Bei einen Land mit 3 Angreifer Truppen kann man immer angreifen
                    if(bc.boardState[bc.playersTerritories[Player_id][i]][1] >= 3)
                    {
                        hasAttackT = 1;
                        ApplyMask(2);

                        break;
                    }
                    //2 Truppen in einem Land mit einem angreifbaren Nachbarland das nur 1 Truppe hat
                    else if (bc.boardState[bc.playersTerritories[Player_id][i]][1] == 2)
                    {
                        for(int j = 0; j < bc.neighboringT[bc.playersTerritories[Player_id][i]].Length; i++)
                        {

                            var nT_id = bc.neighboringT[bc.playersTerritories[Player_id][i]][j];
                            if (bc.boardState[nT_id][1] == 1)
                            {
                                hasAttackT = 1;
                                ApplyMask(2);

                                break;
                            }
                        }
                    }
                }

                //Kein potenzieller Angreifer -> Aktion überspringen
                if (hasAttackT == 0)
                {
                    return 43;
                }

             

            }
            //Verschieben von Truppen J/N
            else
            {
                var canMoveTroups = false;

                //Überprüfe ob es ein Land gibt mit einer Truppenstärke größer als 1 um verschieben zu können
                for (int i = 0; i < bc.playersTerritories[Player_id].Count; i++)
                {
                    if (bc.boardState[bc.playersTerritories[Player_id][i]][1] > 1)
                    {
                        canMoveTroups = true;
                        ApplyMask(2);
                        break;
                    }
                }

                if (canMoveTroups==false)
                {
                    return 43;
                }
            }
        }
        else
        {
            var ownedT = bc.playersTerritories[Player_id];
            ApplyMask(1);

            switch (actionIndex)
            {
                case 2: // Deploy troops
                    for (int i = 0; i < 42; i++)
                    {
                        if (!ownedT.Contains(i))
                        {
                            maskGroup1[i].isActive = false;
                        }
                    }
                    break;

                case 4: // Select attack from
                    for (int i = 0; i < 42; i++)
                    {
                        maskGroup1[i].isActive = false;
                    }
                    foreach (int territory in ownedT)
                    {
                        if (bc.boardState[territory][1] >= 3)
                        {
                            bool hasValidTarget = bc.neighboringT[territory]
                                .Any(n => !ownedT.Contains(n));
                            if (hasValidTarget)
                            {
                                maskGroup1[territory].isActive = true;
                            }
                        }
                        else if (bc.boardState[territory][1] == 2)
                        {
                            bool hasWeakTarget = bc.neighboringT[territory]
                                .Any(n => !ownedT.Contains(n) && bc.boardState[n][1] == 1);
                            if (hasWeakTarget)
                            {
                                maskGroup1[territory].isActive = true;
                            }
                        }
                    }
                    break;

                case 5: // Select attack target
                    int attackerTerritory = (int)input[91]; 
                    for (int i = 0; i < 42; i++)
                    {
                        maskGroup1[i].isActive = false;
                    }
                    foreach (int neighbor in bc.neighboringT[attackerTerritory])
                    {
                        if (!ownedT.Contains(neighbor))
                        {
                            maskGroup1[neighbor].isActive = true;
                        }
                    }
                    break;

                case 8: // Select move from
                    for (int i = 0; i < 42; i++)
                    {
                        maskGroup1[i].isActive = false;
                    }
                    foreach (int territory in ownedT)
                    {
                        if (bc.boardState[territory][1] > 1)
                        {
                            bool hasConnectedPath = bc.neighboringT[territory]
                                .Any(n => ownedT.Contains(n));
                            if (hasConnectedPath)
                            {
                                maskGroup1[territory].isActive = true;
                            }
                        }
                    }
                    break;

                case 9: // Select move to
                    int moveFromTerritory = (int)input[96]; 
                    for (int i = 0; i < 42; i++)
                    {
                        maskGroup1[i].isActive = false;
                    }
                    HashSet<int> reachableTerritories = GetReachableTerritories(moveFromTerritory, ownedT);
                    foreach (int territory in reachableTerritories)
                    {
                        if (territory != moveFromTerritory)
                        {
                            maskGroup1[territory].isActive = true;
                        }
                    }
                    break;
            }
        }

  


        // Set input
        agent.currentInput.Clear();
        agent.currentInput = input;

        // Get prediction
        return agent.Predict();
    }

    private HashSet<int> GetReachableTerritories(int startTerritory, List<int> ownedTerritories)
    {
        HashSet<int> reachable = new HashSet<int>();
        Queue<int> toExplore = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();

        toExplore.Enqueue(startTerritory);
        visited.Add(startTerritory);

        while (toExplore.Count > 0)
        {
            int current = toExplore.Dequeue();
            foreach (int neighbor in bc.neighboringT[current])
            {
                if (ownedTerritories.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    reachable.Add(neighbor);
                    toExplore.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        return reachable;
    }



    public void ApplyMask(int maskGroup)
    {
        // Set all nodes inactive first
        foreach (var node in agent.Kn.Where(k => k.type == 2))
        {
            node.isActive = false;
        }

        // Activate appropriate mask group
        if (maskGroup == 1)
        {
            // Activate territory selection nodes (maskGroup1)
            foreach (var node in maskGroup1)
            {
                node.isActive = true;
            }
        }
        else if (maskGroup == 2)
        {
            // Activate binary decision nodes (maskGroup2)
            foreach (var node in maskGroup2)
            {
                node.isActive = true;
            }
        }
    }



}

