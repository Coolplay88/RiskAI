using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BoardVisualizer : MonoBehaviour
{
    public BoardController controller;

    public GameObject MapPrefab;

    public Button AutoPlay;
    public Button NextRound;
    public Button NextMove;
    public Button NextAction;

    public bool auto;
    public bool oneRound;
    public bool oneMove;
    public bool oneAction;

    public bool log;

    public void Start()
    {
        AutoPlay.GetComponent<Button>().onClick.AddListener(DoAutoPlay);
        NextRound.GetComponent<Button>().onClick.AddListener(DoNextRound);
        NextMove.GetComponent<Button>().onClick.AddListener(DoNextMove);
        NextAction.GetComponent<Button>().onClick.AddListener(DoNextAction);


    }

    public void Update()
    {
        if (controller != null)
        {
            controller.auto = false;
            VisulaizeBoard();
        }
        if (log)
        {
            controller.logActions = true;
        }
        else
        {
            controller.logActions= false;
        }
    }
    public void DoAutoPlay()
    {
        controller.auto = true;
    }
    public void DoNextRound()
    {
        controller.auto = false;
        controller.oneRound = true;
    }
    public void DoNextMove()
    {
        controller.auto = false;
        controller.oneMove = true;
    }
    public void DoNextAction()
    {
        controller.auto = false;
        controller.oneAction = true;
    }

    public void VisulaizeBoard()
    {
        var vsB = new GameObject("Visulaize_"+controller.gameObject.name);
        vsB.transform.parent = transform;

        var map = Instantiate(MapPrefab);
        map.transform.parent = vsB.transform;

        //Spielernamen
        for(int i = 0; i < controller.players.Count; i++)
        {
            map.transform.GetChild(43+i).GetComponent<TextMeshPro>().text= "Spieler"+(i+1)+": "+controller.players[i].name;
        }

        //Rundenzähler
        map.transform.GetChild(42).GetComponent<TextMeshPro>().text = "Runde: " + controller.round;

        //KartenZustandLaden
        for(int i=0; i < controller.boardState.Length; i++)
        {
            var t = map.transform.GetChild(i);

            var tcolor = t.transform.GetChild(0).GetComponent<SpriteRenderer>();
            tcolor.color = getPlayerColor(controller.boardState[i][0]);

            var tTroupCount = t.transform.GetChild(1).GetComponent<TextMeshPro>();
            tTroupCount.text = controller.boardState[i][1].ToString();
        }

    }
    public Color getPlayerColor(int player_id)
    {
        if(player_id == 0)
        {
            return Color.red;
        }
        else if(player_id == 1)
        {
            return Color.blue;
        }
        else if (player_id == 2)
        {
            return Color.green;
        }
        else if (player_id == 3)
        {
            return Color.yellow;
        }

        return Color.black;
    }
}
