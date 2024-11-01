using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class BoardController : MonoBehaviour
{
    public event System.Action OnMissionComplete;
    public event System.Action OnGameEnd;

    private delegate void GameAction();


    //Enthält alle relvanten Daten über den Zustand des Bretts (Besetzerindex , Truppenzahl pro Land)
    public int[][] boardState;

    public List<GameObject> players;

    public GameObject winner;

    public GameObject MapPrefab;

    public int playerCount;

    //Muss gegebenefalls noch für mission_Ids angepasst werden
    private int maxPlayerCount=5;
    
    // #p1 ; #p2 ; #p3 ; #p4 ; #p5 ; 24 GB ; 18GB à 2E ; NA-AU; NA-AF; AS-AF; AS-SA ; EU-AU
    public List<int> mission_ids = new List<int>{0,1,2,3,4,5,6,7,8,9,10,11};
    public List<int> playersMission;

    // Spieler [InfanterieKarten,KavallerieKarten,ArtillerieKarten]
    public List<int[]> playersCards;

    public float[] missionProgress;

    //0: Verteidigung, 1: ggf. KartenBoni einlösen J/N 2:Truppen stationieren 3: Angreifen J/N 4: Angreifen von 5: Angreifen nach 6: TruppenVerschieben(Invasion) 7: AllgemeineTruppenVerschieben J/N 8: TruppenVerschiebenVon 9: TruppenVerschiebenNach
    public int actionIndex;

    public int playerOnMove;

    public int round;

    //Zählt wie oft eine Kartenserie schon eingelöst wurde
    public int cardBoniCounter;

    public bool gameDone;

    //Länder eines Spielers
    public List<List<int>> playersTerritories;

    public List<int[]> neighboringT;

    //Wenn gerade analysiert wird von jmd. sollen Aktionen auch einzeln durchgeführt werden:

    //Spielt automatisch das Game bis zum Ende
    public bool auto;
    //Spielt automatisch eine Runde (alle Spieler)
    public bool oneRound;
    //Spielt automatisch einen Zug (ein Spieler)
    public bool oneMove;
    //Spielt automatisch eine Aktion (eine Aktion)
    public bool oneAction;

    //Printen von Informationen die Passiert sind:
    public bool logActions;

    public void Start()
    {

       gameDone = false;

        cardBoniCounter = 0;

        playersCards = new List<int[]>();

        players = new List<GameObject>();

        playersTerritories = new List<List<int>>();

        //Aufsetzen des Arrays boardState
        boardState = new int[42][];
        
        for(int i = 0; i < boardState.Length; i++)
        {
            boardState[i] = new int[2];
          
        }

        for(int i = 0; i < playerCount; i++)
        {
            playersCards[i] = new int[3];
        }





        //Einstellen der benachbarten Territorien

        neighboringT = new List<int[]>();

        //Muss auch in der Hierachy so eingestellt sein !

        // Nordamerika
        neighboringT.Add(new int[] { 1, 2, 29 }); // 0: Alaska
        neighboringT.Add(new int[] { 0, 2, 3, 7 }); // 1: Nordwest-Territorien
        neighboringT.Add(new int[] { 0, 1, 3, 5 }); // 2: Alberta
        neighboringT.Add(new int[] { 1, 2, 4, 5, 6, 7 }); // 3: Ontario
        neighboringT.Add(new int[] { 3, 6, 7 }); // 4: Östliches-Kanada
        neighboringT.Add(new int[] { 2, 3, 6, 8 }); // 5: Weststaaten
        neighboringT.Add(new int[] { 3, 4, 5, 8 }); // 6: Oststaaten
        neighboringT.Add(new int[] { 1, 3, 4, 24 }); // 7: Grönland
        neighboringT.Add(new int[] { 5, 6, 9 }); // 8: Mittelamerika

        // Südamerika
        neighboringT.Add(new int[] { 8, 10, 11 }); // 9: Venezuela
        neighboringT.Add(new int[] { 9, 11, 12 }); // 10: Peru
        neighboringT.Add(new int[] { 9, 10, 12, 13 }); // 11: Brasilien
        neighboringT.Add(new int[] { 10, 11 }); // 12: Argentinien

        // Afrika
        neighboringT.Add(new int[] { 11, 14, 15, 16, 19, 20 }); // 13: NordAfrika
        neighboringT.Add(new int[] { 13, 16, 20, 36 }); // 14: Ägypten
        neighboringT.Add(new int[] { 13, 16, 18 }); // 15: ZentralAfrika
        neighboringT.Add(new int[] { 13, 14, 15, 17, 18, 36 }); // 16: Ostafrika
        neighboringT.Add(new int[] { 16, 18 }); // 17: Madagaskar
        neighboringT.Add(new int[] { 15, 16, 17 }); // 18: Südafrika

        // Europa
        neighboringT.Add(new int[] { 13, 20, 21, 23 }); // 19: Westeuropa
        neighboringT.Add(new int[] { 13, 14, 19, 21, 25, 36 }); // 20: Südeuropa
        neighboringT.Add(new int[] { 19, 20, 22, 23, 25 }); // 21: Nordeuropa
        neighboringT.Add(new int[] { 21, 23, 24, 25 }); // 22: Skandinavien
        neighboringT.Add(new int[] { 19, 21, 22, 24 }); // 23: Großbritannien
        neighboringT.Add(new int[] { 7, 22, 23 }); // 24: Island
        neighboringT.Add(new int[] { 20, 21, 22, 26, 35, 36 }); // 25: Russland

        // Asien
        neighboringT.Add(new int[] { 25, 27, 32, 35, }); // 26: Ural
        neighboringT.Add(new int[] { 26, 28, 30, 31, 32 }); // 27: Sibirien
        neighboringT.Add(new int[] { 27, 29, 30 }); // 28: Jakutien
        neighboringT.Add(new int[] { 0, 28, 30, 31, 37 }); // 29: Kamtschatka
        neighboringT.Add(new int[] { 27, 28, 29, 31 }); // 30: Irkutsk
        neighboringT.Add(new int[] { 27, 29, 30, 32, 37 }); // 31: Mongolei
        neighboringT.Add(new int[] { 26, 27, 31, 33, 34, 35 }); // 32: China
        neighboringT.Add(new int[] { 32, 34, 38 }); // 33: SüdOstAsien
        neighboringT.Add(new int[] { 32, 33, 35, 36 }); // 34: Indien
        neighboringT.Add(new int[] { 25, 26, 32, 34, 36 }); // 35: Afghanistan
        neighboringT.Add(new int[] { 14, 16, 20, 25, 34, 35 }); // 36: Naher Osten
        neighboringT.Add(new int[] { 29, 31 }); // 37: Japan


        // Australien
        neighboringT.Add(new int[] { 33, 39, 40 }); // 38: Indonesien
        neighboringT.Add(new int[] { 38, 41 }); // 39: Neuguinea
        neighboringT.Add(new int[] { 38, 41 }); // 40: Westaustralien
        neighboringT.Add(new int[] { 39, 40 }); // 41: Ostaustralien
    }


    public void InnitializeGame()
    {
        //Jetzt wurden Spieler schon ins Array players hinzugefügt -> Vermittlung des BoardControllers an die einzelnen Agent_Handler zur validen Zugwahl
        for(int i = 0; i < players.Count; i++)
        {
            players[i].GetComponent<AgentHandler>().bc = gameObject.GetComponent<BoardController>();
        }

        //Gebietsverteilung
        DistributeTerritories();
        //Missionsverteilung
        DistributeMission();

        //Starten des Spiels
        PlayGame();
  
    }
    //Spielen des Spiels
    public void PlayGame()
    {
        gameDone = false;
        round = 0;
        cardBoniCounter = 0;
        playerOnMove = SelectStarter();

        if (auto)
        {
            PlayFullGame();
        }
    }

    private void PlayFullGame()
    {
        while (round <= 70 && !gameDone)
        {
            PlayRound();
        }
    }

    private void PlayRound()
    {
        if (!oneRound && !auto) return;

        for (int i = 0; i < playerCount; i++)
        {
            PlayMove(playerOnMove);

            playerOnMove = (playerOnMove + 1) % playerCount;
        }
        round++;
        oneRound = false;
    }

    private void PlayMove(int playerId)
    {
        if (!oneMove && !oneRound && !auto) return;

        // Execute player's turn actions
        DistributeTroups(playerId);
        Attack(playerId);
        MoveTroupsGeneral(playerId);

        oneMove = false;
    }

    private void ExecuteAction(Action action)
    {
        if (!oneAction && !oneMove && !oneRound && !auto) return;

        action.Invoke();
        oneAction = false;
    }

    public void DistributeTroups(int Player_id)
    {
        ExecuteAction(() => {
            actionIndex = 2;
            var troupCount = GetTroupCount(Player_id, false);

            for (int i = 0; i < troupCount; i++)
            {
                var placeAtT_id = players[Player_id]
                    .GetComponent<AgentHandler>()
                    .NNPredict(GetInput(Player_id, false, null, -1, -1, -1), actionIndex, Player_id);
                AddUnit(placeAtT_id);

                //ggf. log
                if (logActions)
                {
                    Debug.Log(players[Player_id].name + " hat eine Truppe auf " + MapPrefab.transform.GetChild(placeAtT_id).name + " platziert");
                }
            }
        });
    }


    public void Attack(int Player_id)
    {
        ExecuteAction(() => {
            var sucAttCounter = 0;
            var ag = players[Player_id].GetComponent<AgentHandler>();

            actionIndex = 3;

            while (ag.NNPredict(GetInput(Player_id, false, null, -1, -1, -1), actionIndex, Player_id) == 42)
            {
                actionIndex = 4;
                var attackerT_id = ag.NNPredict(GetInput(Player_id, false, null, -1, -1, -1), actionIndex, Player_id);

                actionIndex = 5;
                var defenderT_id = ag.NNPredict(GetInput(Player_id, false, null, attackerT_id, -1, -1), actionIndex, Player_id);

                Defend(attackerT_id, defenderT_id, sucAttCounter);

                //ggf. log
                if (logActions)
                {
                    Debug.Log(players[Player_id].name + " hat von " + MapPrefab.transform.GetChild(attackerT_id).name + " aus " + MapPrefab.transform.GetChild(defenderT_id).name+" angegriffen");                  
                }
            }
        });
    }

    public void Defend(int attackerT_id,int defenderT_id, int sucAttCounter)
    {

        //Der Verteidigende Spieler
        var defender_id = boardState[defenderT_id][0];
        var defender = players[defender_id];

        //Für späteren Methodenaufruf notwendig
        var attacker_id = boardState[attackerT_id][0];

        //Angreifer WürfelAugen
        int[] attackerDice = new int[3];

        //Angreifer Würfelt
        //Mit 3 wenn er kann
        if (boardState[attackerT_id][1] >= 3)
        {
            attackerDice[0]=UnityEngine.Random.Range(1,7);
            attackerDice[1]=UnityEngine.Random.Range(1,7);
            attackerDice[2]=UnityEngine.Random.Range(1,7);
        }
        //Sonst nur mit 2 aber es brauch mindestens 2 alle anderen wurden schon maskiert
        else
        {
            attackerDice[0] = 0;
            attackerDice[1] = UnityEngine.Random.Range(1,7);
            attackerDice[2] = UnityEngine.Random.Range(1,7);
        }

        //Verteidiger Würfelaugen
        int[] defenderDice = new int[2];

        //Wenn Defender mehr als 1 Verteidiger hat
        if (boardState[defenderT_id][1] > 1)
        {
            //Verteidiger entscheidet ob er mit Zwei oder einen Würfel verteidigt (actionIndex=0)
            actionIndex = 0;

            var r = defender.GetComponent<AgentHandler>().NNPredict(GetInput(defender_id, false, attackerDice,attackerT_id,defenderT_id,-1), actionIndex,defender_id);

            //Mit 2 Würfeln
            if (r == 42)
            {
                defenderDice[0] = UnityEngine.Random.Range(1,7);
                defenderDice[1]= UnityEngine.Random.Range(1,7);
            }
            //Mit 1 Würfel
            else
            {
                defenderDice[0] = 0;
                defenderDice[1] = UnityEngine.Random.Range(1, 7);
            }
        }
        //Ergebnis der Verteidigung feststellen (0: Attacker hat Gewonnen, 1: Defender hat Gewonnen 2: Unentschieden) 
        int resultOfDefendence = 0;
        var pointsA = 0;
        var pointsD = 0;

        attackerDice = attackerDice.OrderByDescending(x => x).ToArray();
        defenderDice = defenderDice.OrderByDescending(x => x).ToArray();

        //Bei zwei Verteidigungswürfeln
        if (defenderDice[1] != 0)
        {
            //Höchst vgl.
            if (attackerDice[0] > defenderDice[0])
            {
                pointsA++;
            }
            else if(attackerDice[0] <= defenderDice[1])
            {
                pointsD++;
            }
            //Zweithöchste vgl.
            if (attackerDice[1] > defenderDice[1])
            {
                pointsA++;
            }
            else if (attackerDice[1] <= defenderDice[1])
            {
                pointsD++;
            }        
        }
        //Bei einem Verteidigungswürfel
        else
        {
            //Höchst vgl.
            if (attackerDice[0] > defenderDice[0])
            {
                pointsA++;
            }
            else if (attackerDice[0] <=defenderDice[1])
            {
                pointsD++;
            }
        }

        if (pointsA > pointsD)
        {
            resultOfDefendence = 0;
        }
        else if(pointsA< pointsD)
        {
            resultOfDefendence = 1;
        }
        else
        {
            resultOfDefendence = 2;
        }

        //Ausertung der Verteidigungsergebnisse

        //Angreifer gewinnt oder Unentschieden
        if (resultOfDefendence == 0|| resultOfDefendence==2)
        {
            if (resultOfDefendence == 2)
            {
                //Bei unentschieden auch noch eine AngreiferTruppe entfernen
                RemoveUnit(attacker_id);

           
            }

            //Entfernen einer Verteidigungseinheit
            RemoveUnit(defenderT_id);

            //Wenn keine VerteidigerTruppen mehr stehen dann soll Angreifer Truppen ziehen (Erfolgreiche Landeinahme)
            if (boardState[defenderT_id][1] == 0)
            {
                //Muss mindestens mit einer Truppe besetzen
                AddUnit(defenderT_id);
                //Ändern des Besetzers
                boardState[defenderT_id][0] = attacker_id;

                //Ändern der Territorien zugehörigkeit
                playersTerritories[defender_id].Remove(defenderT_id);
                playersTerritories[attacker_id].Add(defenderT_id);

                //Angreifer kann mit weiteren Truppen nachziehen
                MoveTroupsInvading(attackerT_id,defenderT_id,attacker_id);

                //Erhalte BonusKarte (nur für mind. 1 erfolgreiche Einnahme)
                if (sucAttCounter == 0)
                {
                    sucAttCounter++;

                    var cardType = UnityEngine.Random.Range(0, 4);

                    //Hinzufügen der neune Karte
                    playersCards[attacker_id][cardType]++;
                }
            }
        }
        //Verteidiger gewinnt
        else if(resultOfDefendence == 1)
        {
            //Entfernen einer Angriffstruppe
            RemoveUnit(attackerT_id);
        }
       

        //Am Ende ist der Angriffsprozess abgeschlossen (actionIndex=2) erneute Nachfrage und Prüfung nach weiteren Angriff
        actionIndex = 2;

    }
    public void MoveTroupsInvading(int attackerT_id,int defenderT_id, int Player_id)
    {
        //Aktionsindex 6

        actionIndex = 6;

        //KI fragen ob es eine weitere Truppe in das neu Eingenommene Territorium ziehen möchte
        while (players[Player_id].GetComponent<AgentHandler>().NNPredict(GetInput(Player_id, false, null,-1,-1,attackerT_id), actionIndex,Player_id) == 42)
        {
            //Remove eine vom Angreifer Land
            RemoveUnit(attackerT_id);

            //Füge eine im neu eingenommenne hinzu
            AddUnit(defenderT_id);

        }
    }
    public void MoveTroupsGeneral(int Player_id)
    {
        ExecuteAction(() => {
            actionIndex = 7;

            while (players[Player_id].GetComponent<AgentHandler>().NNPredict(GetInput(Player_id, false, null, -1, -1, -1), actionIndex, Player_id) == 42)
            {
                actionIndex = 8;
                var fromT_id = players[Player_id].GetComponent<AgentHandler>().NNPredict(GetInput(Player_id, false, null, -1, -1, -1), actionIndex, Player_id);

                actionIndex = 9;
                var toT_id = players[Player_id].GetComponent<AgentHandler>().NNPredict(GetInput(Player_id, false, null, -1, -1, fromT_id), actionIndex, Player_id);

                RemoveUnit(fromT_id);
                AddUnit(toT_id);

                actionIndex = 7;

                //ggf. log
                if (logActions)
                {
                    Debug.Log(players[Player_id].name + " hat Truppe von " + MapPrefab.transform.GetChild(fromT_id).name + " nach " + MapPrefab.transform.GetChild(toT_id).name + " verschoben");
                }
            }
        });
    }

    public void DistributeTerritories()
    {
        //Liste initialisieren
        playersTerritories = new List<List<int>>();

        for (int i = 0; i < players.Count; i++)
        {
            playersTerritories[i] = new List<int>();
        }

        //Fixe Anzahl kann verteilt werden

        var accesibleTerritories = Enumerable.Range(0, 42).ToList();
        var fixedCount = Mathf.Floor(boardState.Length / playerCount);

        for(int i = 0; i < boardState.Length; i++)
        {
            if (i <= playerCount * fixedCount)
            {
                //Der Reihe nach bei fixer Anzahl
                var player_id = i % playerCount;
                var chosenTerritory = accesibleTerritories[UnityEngine.Random.Range(0,accesibleTerritories.Count)];

                //Hinzufügen in Territory Liste für den Spieler
                playersTerritories[player_id].Add(chosenTerritory);

                //AktualisierenDesBoardStates Besitzer
                boardState[chosenTerritory][0] = player_id;

                //Entfernen aus Verfügbar Liste
                accesibleTerritories.Remove(chosenTerritory);
            }

            //Alle Spieler haben gleich viel
            //Zufällige Restverteilung
            var playerIds = Enumerable.Range(0, playerCount-1).ToList();

            for(float j = playerCount*fixedCount; j < boardState.Length; j++)
            {
                var chosenPlayer = playerIds[UnityEngine.Random.Range(0,playerIds.Count)];

                var chosenTerritory = accesibleTerritories[UnityEngine.Random.Range(0, accesibleTerritories.Count)];

                playersTerritories[chosenPlayer].Add(chosenTerritory);

                //AktualisierenDesBoardStates Besitzer
                boardState[chosenTerritory][0] = chosenPlayer;

                accesibleTerritories.Remove(chosenTerritory);
                playerIds.Remove(chosenPlayer);
            }


        }
    }
    public void DistributeMission()
    {
        //Zerstören sie p Karten herausnehmen wenn p > Anzahl Spieler

        for(int i = playerCount;i<maxPlayerCount;i++)
        {
            mission_ids.Remove(playerCount + i);
        }


        playersMission = new List<int>();

        //Für jeden Spieler
        for(int i = 0; i < playerCount; i++)
        {
            var chosenMission = mission_ids[UnityEngine.Random.Range(0,mission_ids.Count)];

            mission_ids.Remove(chosenMission);

            //Selbsteliminierung Karte -> 24 Gebiete ihrer Wahl
            if(chosenMission==mission_ids[i])
            {
                chosenMission = mission_ids[playerCount];
            }

            //Kann neu geaddet werden da mit leerer Liste gestartet wird
            playersMission.Add(chosenMission);
        }
    }
    public int SelectStarter()
    {
        var starterId = UnityEngine.Random.Range(0,playerCount-1);

        return starterId;
    }

    public void AddUnit(int Territory_Index)
    {
        boardState[Territory_Index][1]++;
    }
    public void RemoveUnit(int Territory_Index)
    {
        boardState[Territory_Index][1]--;
    }
    public float CheckMissionProgress(int Player_id)
    {
        var mission_id = playersMission[Player_id];

        var progress = 0f;

        //EliminierungsMissionen
        if(mission_id < playerCount)
        {
            progress = 1-((playersTerritories[mission_id].Count)/(boardState.Length));

            
        }
        //GebietsanzahlMissionen
        else if(mission_id>playerCount && mission_id < playerCount + 3)
        {
            //24 Gebiete ihrer Wahl
            if (mission_id == playerCount + 1)
            {
                progress = (playersTerritories[Player_id].Count)/24;
            }
            //18 Gebiete à 2 Einheiten ihrer Wahl
            else
            {
                var Tcount = 0;

                for(int i = 0; i < playersTerritories[Player_id].Count; i++)
                {
                    var t = playersTerritories[Player_id][i];   

                    if(boardState[t][1] >= 2)
                    {
                        Tcount++;
                    }
                }

                progress = Tcount / 18;
            }
        }
        //Kontinenteroberungen
        else
        {
            var TcountNA = 0;
            var TcountAU = 0;
            var TcountAF = 0;
            var TcountAS = 0;
            var TcountSA = 0;
            var TcountEU = 0;


            for (int i = 0; i < playersTerritories[Player_id].Count; i++)
            {
                var t = playersTerritories[Player_id][i];

                if (t<=8)
                {
                    TcountNA++;
                }
                else if (t <= 12)
                {
                    TcountSA++;
                }
                else if(t <= 18)
                {
                    TcountAF++;
                }
                else if (t <= 25)
                {
                    TcountEU++;
                }
                else if (t <= 37)
                {
                    TcountAS++;
                }
                else
                {
                    TcountAU++;
                }
            }

            //NA
            if (mission_id < playerCount + 5)
            {

             

                //AU
                if (mission_id == playerCount + 3)
                {
                    progress = (TcountNA+TcountAU) / (9+4);
                }
                //AF
                else
                {
                    progress = (TcountNA + TcountAF) / (9 + 6);
                }
            }
            //AS
            else if(mission_id < playerCount + 7)
            {
                //AF
                if (mission_id == playerCount + 5)
                {
                    progress = (TcountAS + TcountAF) / (12 + 6);
                }
                //SA
                else
                {
                    progress = (TcountAS + TcountSA) / (12 + 4);
                }
            }
            //EU
            else
            {
                progress = (TcountEU + TcountAU) / (7+4);
            }
        }


        if (progress == 1)
        {
            winner = players[Player_id];
            gameDone = true;
        }

        return progress;
    }


    // Constants for continent definitions and bonuses
    private readonly Dictionary<string, (int[] territories, int bonus)> CONTINENTS = new Dictionary<string, (int[] territories, int bonus)>
    {
    {"NorthAmerica", (new int[] {0,1,2,3,4,5,6,7,8}, 5)},
    {"SouthAmerica", (new int[] {9,10,11,12}, 2)},
    {"Africa", (new int[] {13,14,15,16,17,18}, 3)},
    {"Europe", (new int[] {19,20,21,22,23,24,25}, 5)},
    {"Asia", (new int[] {26,27,28,29,30,31,32,33,34,35,36,37}, 7)},
    {"Australia", (new int[] {38,39,40,41}, 2)}
    };
    public float GetTroupCount(int Player_id,bool evaluatesCardBoni)
    {
        var totalTroupCount = 0f;

        //BasisTruppen
        var baseTroupCount =Mathf.Max(3, Mathf.Floor(playersTerritories[Player_id].Count / 3));

        //KontinentBoni
        var continentBoni = 0f;

        // Check each continent for complete control
        foreach (var continent in CONTINENTS)
        {
            bool controlsContinent = continent.Value.territories
                .All(territory => playersTerritories[Player_id].Contains(territory));

            if (controlsContinent)
            {
                continentBoni += continent.Value.bonus;
            }
        }

        //Überprüfe auf KartenSerie und Frage KI nach Einlösen

        //Bonus durch Kartenserie
        var cardBoni = 0;

        //Nur wenn KI nicht gerade bewertet ob sie Kartenserie einlösen soll
        if (evaluatesCardBoni == false)
        {
           

            var hasCardBoni = false;
            //Verschiedene Kartenarten
            var countCardTypes = 0;
            //0: 3 von Infa. ,1: 3 von Kav, 2: 3 von Art, 4: 1 von jeder Sorte
            var kindOfCardCombi = 0;

            for (int i = 0; i < playersCards[Player_id].Length; i++)
            {
                //Von einer Sorte mindestens 3
                if (playersCards[Player_id][i] >= 3)
                {
                    hasCardBoni = true;

                    kindOfCardCombi = i;
                }
                //Von jeder Sorte mindestens 1
                if (playersCards[Player_id][i] > 0)
                {
                    countCardTypes++;
                }
            }
            if (countCardTypes == 3)
            {
                hasCardBoni = true;

                //1 von jeder Sorte
                kindOfCardCombi = 4;
            }

            //Wenn KI eine mögliche Kartenserie einlösen kann
            if (hasCardBoni)
            {
                //Frage KI nach Einlösen (actionIndex =1 )

                actionIndex = 1;

                var ag = players[Player_id].GetComponent<AgentHandler>();
                //action index hier separat für Maskierungserleichterung der OutputNeuronen
                var resultPrediction = ag.NNPredict(GetInput(Player_id, true,null,-1,-1,-1), actionIndex,Player_id);

                //OutputNeuron Index!!! 42 entspricht der Durchführung einer Aktion
                if (resultPrediction == 42)
                {
                    if (cardBoniCounter <= 5)
                    {
                        cardBoni = 2 * (cardBoniCounter + 1);
                        cardBoniCounter++;
                    }
                    else
                    {
                        cardBoni = 5 * (cardBoniCounter - 3);
                        cardBoniCounter++;
                    }

                    //Karten nach Einlösen löschen
                    if (kindOfCardCombi == 4)
                    {
                        playersCards[Player_id][0]--;
                        playersCards[Player_id][1]--;
                        playersCards[Player_id][2]--;
                    }
                    else
                    {
                        playersCards[Player_id][kindOfCardCombi]--;
                    }

                    //ggf. log
                    if (logActions)
                    {
                        Debug.Log(players[Player_id].name + " hat eine Kartenserie eingelöst");
                    }

                }
            }
        }

        //Zusammenaddieren aller Boni
        totalTroupCount = baseTroupCount + continentBoni + cardBoni;

        return totalTroupCount;
    }
    public List<float> GetInput(int Player_id, bool evaluatesCardBoni,int[] attackerDice,int attackerT_id,int defenderT_id,int moveFromT_id)
    {
        var input = new List<float>();

        //Input festlegen

        //Bias
        input.Add(1);

        //Brett Zustand (42 Länder mit Besetzer_ID und Truppenstärke)
        for(int i = 0; i < boardState.Length; i++)
        {
            for(int j=0;j< boardState[i].Length; j++)
            {
                input.Add(boardState[i][j]);
            }
        }
        //Aktions und Missionsindex
        input.Add(actionIndex);
        input.Add(playersMission[Player_id]);

        //Missionsfortschritt
        input.Add(CheckMissionProgress(Player_id));

        if (evaluatesCardBoni)
        {
            //Erhält folgende Truppenstärke
            input.Add(GetTroupCount(Player_id,true));
        }
        else
        {
            //Erhält folgende Truppenstärke
            input.Add(GetTroupCount(Player_id,false));
        }

        //Anzahl eingelöster Serien
        input.Add(cardBoniCounter);

        //Anzal an gespielten Runden
        input.Add(round);

        //Angreifendes Land (nur Verteidigung)
        input.Add(attackerT_id);

        //Verteidigendes Land (nur Verteidigung)
        input.Add(defenderT_id);

        //Augenwürfel Angreifer (nur bei Verteidigung)
        if(actionIndex == 0)
        {
            for(int i = 0; i < 3; i++)
            {
                if (i >= attackerDice.Length)
                {
                    input.Add(0);
                }
                else
                {
                    input.Add(attackerDice[i]);
                }
            }
        }
        else
        {
            for(int i = 0; i < 3; i++)
            {
                input.Add(0);
            }
        }


        //Land von dem Aus Truppe verschoben wird
        input.Add(moveFromT_id);


        //Eigene Karten und Karten der anderen Spieler
        for (int i = 0; i < playersCards.Count; i++)
        {
            for(int j=0; j<playersCards[i].Length; j++)
            {
                input.Add(playersCards[i][j]);
            }
        }


        //Wie viele Kartensätze schon eingelöst wurden
        input.Add(cardBoniCounter);


        return input;
    }

}
