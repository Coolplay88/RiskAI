using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TournamentCreator : MonoBehaviour
{
    public Population_Manager manager;
    public int playerCountPerBoard = 4;
    public int levels=3;
    public int GamesPerBoard=100;
    public int gamesDone = 0;

    private Dictionary<Genome, List<float>> missionProgressTracker = new Dictionary<Genome, List<float>>();
    private Dictionary<Genome, List<bool>> winTracker = new Dictionary<Genome, List<bool>>();

    public void CreateTournament(int GamesPerBoard, int levels, int playerCountPerBoard)
    {
        this.GamesPerBoard = GamesPerBoard;
        this.levels = levels;
        this.playerCountPerBoard = playerCountPerBoard;

        // Create tournament structure
        for (int level = 0; level < levels; level++)
        {
            var gamesInLevel = new GameObject($"Level_{level}");
            gamesInLevel.transform.parent = transform;

            // Create groups of players that will play multiple games together
            for (int constellation = 0; constellation < GamesPerBoard; constellation++)
            {
                var playerGroup = new List<GameObject>();

                // Create players for this constellation
                for (int i = 0; i < playerCountPerBoard; i++)
                {
                    var player = new GameObject($"Player_{i}");
                    var agentHandler = player.AddComponent<AgentHandler>();
                    var genome = manager.CreateRandomGenome().GetComponent<Genome>();
                    agentHandler.agent = genome;
                    playerGroup.Add(player);

                    // Initialize tracking for this genome
                    if (!missionProgressTracker.ContainsKey(genome))
                    {
                        missionProgressTracker[genome] = new List<float>();
                        winTracker[genome] = new List<bool>();
                    }
                }

                // Create multiple games for this constellation
                for (int gameNumber = 0; gameNumber < GamesPerBoard; gameNumber++)
                {
                    var gameBoard = new GameObject($"Constellation_{constellation}_Game_{gameNumber}");
                    gameBoard.transform.parent = gamesInLevel.transform;

                    var gameHandler = gameBoard.AddComponent<GameHandler>();
                    gameHandler.playerCountPerBoard = playerCountPerBoard;

                    var boardController = gameBoard.AddComponent<BoardController>();
                    boardController.players = new List<GameObject>(playerGroup);

                    // Add game end listener
                    boardController.OnGameEnd += () => UpdateFitness(boardController);

                    boardController.InnitializeGame();
                }
            }
        }
    }

    private void UpdateFitness(BoardController board)
    {
        foreach (var player in board.players)
        {
            var agentHandler = player.GetComponent<AgentHandler>();
            var genome = agentHandler.agent;

            if (genome != null)
            {
                // Track mission progress
                missionProgressTracker[genome].Add(agentHandler.missionProgress);

                // Track wins
                winTracker[genome].Add(agentHandler.hasWon);

                // Calculate and update fitness
                float avgMissionProgress = missionProgressTracker[genome].Average();
                float winRate = winTracker[genome].Count > 0 ?
                    winTracker[genome].Count(w => w) / (float)winTracker[genome].Count : 0;

                genome.fitness = avgMissionProgress + winRate;
            }
        }
    }

    public void Update()
    {
        int totalGames = levels * GamesPerBoard;
        gamesDone = CountCompletedGames();

        if (gamesDone == totalGames)
        {
            manager.EndGeneration();
            gamesDone = 0;

            // Clean up tournament
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }
    }


    private int CountCompletedGames()
    {
        int completed = 0;
        foreach (Transform level in transform)
        {
            foreach (Transform game in level)
            {
                var boardController = game.GetComponent<BoardController>();
                if (boardController && boardController.gameDone)
                {
                    completed++;
                }
            }
        }
        return completed;
    }
}
