using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Population_Manager : MonoBehaviour
{
    // References

    public TournamentCreator creator;

    // Basic Network Parameters
    public int inputN = 1+2*42 +1+1+1+1+1+1+1+1+3+1;  // Bias , 42 Territories mit Besetzer und Truppenstärke , Aktionsindex, Missionsindex, Missionsfortschritt, Truppenverstärkung, Anzahl eingelöster Kartenserien, Rundenanzahl,AngreiferLand (0,4),VerteidigerLand(0), Angreifer Würfelaugen,ZiehenVonLand(8)
    public int outputN = 42 +1 +1; // 42 Territorien bei Angriff/Verschieben, Aktion durchführen, Aktion überspringen
    public int popCount = 64;                // Population size
    public int gen;                           // Current generation

    // Species Parameters
    public float c1 = 1.0f;                   // Excess weight
    public float c2 = 1.0f;                   // Disjoint weight  
    public float c3 = 0.4f;                   // Weight diff weight
    public float deltaNS = 3.0f;              // Initial species threshold
    public int targetSpeciesCount = 15;       // Target number of species
    public float interspeciesMatingRate = 0.01f;

    // Mutation Rates
    public float pNKn = 0.03f;                // New node probability
    public float pRKn = 0.01f;                // Remove node probability
    public float pNKa = 0.05f;                // New connection probability
    public float pKaA = 0.1f;                 // Disable connection probability
    public float pNW = 0.8f;                  // Weight mutation probability
    public float percNW = 0.1f;               // Weight mutation size
    public float pChangeActivation = 0.1f;    // Activation function mutation rate
    public float pKaAC = 0.25f;               // Connection enable rate
    public float percOfDeactivation = 0.2f;   // Initial connection disable rate

    //Base Mutation Rates
    private float baseNKn;
    private float baseNW;
    private float baseNKa;
    private float baseRNKn;
    private float baseChangeActivation;
    private float baseKaAC;


    // Evolution Parameters
    public float elitePerc = 0.1f;            // Elite percentage
    public float Lernrate = 0.1f;             // Learning rate
    public float survivalThreshold = 0.2f;    // Survival rate

    // Innovation & Protection
    public int baseInnovation = 1000;         // Innovation number base
    public int speciesCounter;                // Species counter
    public int spawnProtection = 5;           // Species protection period
    public int innovationProtectionPeriod = 10;

    // Species Threshold Parameters
    public float initialDeltaNS = 3.0f;
    public float deltaNSDecayRate = 0.995f;
    public float minDeltaNS = 0.3f;
    public float initialCompatibilityThreshold = 3.0f;
    public float compatibilityThresholdDelta = 0.1f;

    // Runtime Variables
    public bool GenNow;
    private float currentCompatibilityThreshold;
    private Dictionary<int, int> innovationAge = new Dictionary<int, int>();
    public List<GameObject> popOfGenome = new List<GameObject>();
    public List<Species> speciesList = new List<Species>();


    //Fitness Tracking
    public GameObject allTimeBestGenome;
    public float allTimeBestFitness = float.MinValue;


    public bool perfectFound;

    private void UpdateDeltaNS()
    {
        deltaNS = Mathf.Max(initialDeltaNS * Mathf.Pow(deltaNSDecayRate, gen), minDeltaNS);
    }

    public void Start()
    {
        inputN += 3 * creator.playerCountPerBoard; //Karten jedes Spielers

        //Base Mutation Rate = Mutation Rates
        baseNW = pNW;
        baseNKn = pNKn;
        baseNKa = pNKa;
        baseRNKn = pRKn;
        baseKaAC = pKaAC;
        baseChangeActivation = pChangeActivation;

        popOfGenome = new List<GameObject>();
        currentCompatibilityThreshold = initialCompatibilityThreshold;
        speciesCounter = 0;
        GenNow = false;
        gen = 0;
        perfectFound = false;
        InitializeStarterPopulation();
    }

    public void Update()
    {
        if (GenNow && perfectFound == false)
        {
            GenNow = false;
            foreach (var genomeObj in popOfGenome)
            {
                Mutation(genomeObj.GetComponent<Genome>());
            }
            TestGenomes();
            KillWeakestPerSpecies();
            Repopulate();
           





        }

    }

    private void AdjustSpeciesCount()
    {
        int currentSpeciesCount = speciesList.Count;

        // Dynamische Anpassung des Compatibility Threshold
        if (currentSpeciesCount > targetSpeciesCount)
        {
            currentCompatibilityThreshold += compatibilityThresholdDelta;
        }
        else if (currentSpeciesCount < targetSpeciesCount)
        {
            currentCompatibilityThreshold = Mathf.Max(
                currentCompatibilityThreshold - compatibilityThresholdDelta,
                0.1f
            );
        }

        // Aggressive Anpassung bei großer Abweichung
        if (Mathf.Abs(currentSpeciesCount - targetSpeciesCount) > 5)
        {
            compatibilityThresholdDelta *= 1.2f;
        }
        else
        {
            compatibilityThresholdDelta = 0.1f;
        }

        // Begrenzung des Thresholds
        currentCompatibilityThreshold = Mathf.Clamp(
            currentCompatibilityThreshold,
            0.1f,
            5f
        );
    }


    private void UpdateInnovationAges()
    {
        foreach (var kvp in innovationAge.ToList())
        {
            innovationAge[kvp.Key]++;
            if (innovationAge[kvp.Key] > innovationProtectionPeriod)
            {
                innovationAge.Remove(kvp.Key);
            }
        }
    }

    public class Species
    {
        public int id;
        public int age;
        public float bestFitness;
        public int generationsWithoutImprovement;
        public List<Genome> members;

        public Species(int id)
        {
            this.id = id;
            age = 0;
            bestFitness = float.MinValue;
            generationsWithoutImprovement = 0;
            members = new List<Genome>();
        }

        public void UpdateFitness(float newBestFitness)
        {
            if (newBestFitness > bestFitness)
            {
                bestFitness = newBestFitness;
                generationsWithoutImprovement = 0;
            }
            else
            {
                generationsWithoutImprovement++;
            }
            age++;
        }
    }

    public void GetSpecies(Genome g)
    {
        foreach (var species in speciesList)
        {
            if (species.members.Count == 0) continue;
            var representativeGenome = species.members[0];
            var speciesValue = CalculateSpeciesValue(g, representativeGenome);
            if (speciesValue < currentCompatibilityThreshold)
            {
                g.species = species.id;
                species.members.Add(g);
                return;
            }
        }

        speciesCounter++;
        var newSpecies = new Species(speciesCounter);
        newSpecies.members.Add(g);
        g.species = speciesCounter;
        speciesList.Add(newSpecies);

        AdjustSpeciesCount();

        foreach (var kante in g.Ka)
        {
            if (!innovationAge.ContainsKey(kante.innovation_number))
            {
                innovationAge[kante.innovation_number] = 0;
            }
        }
    }


    public List<List<Kanten>> CrossingGenes(Genome g1, Genome g2)
    {
        var crossingGenes = new List<List<Kanten>>();

        var matchingGenes1 = new List<Kanten>();
        var matchingGenes2 = new List<Kanten>();

        var disjointGenes1 = new List<Kanten>();
        var disjointGenes2 = new List<Kanten>();
        var excessGenes = new List<Kanten>();

        var maxInnoG1 = 0;
        var maxInnoG2 = 0;

        // Ermittlung der maximalen Innovationsnummern
        foreach (var kante in g1.Ka)
        {
            if (kante.innovation_number > maxInnoG1)
            {
                maxInnoG1 = kante.innovation_number;
            }
        }
        foreach (var kante in g2.Ka)
        {
            if (kante.innovation_number > maxInnoG2)
            {
                maxInnoG2 = kante.innovation_number;
            }
        }



        // Kanten von g1 klassifizieren
        foreach (var kante in g1.Ka)
        {
            var matchingKanteG2 = g2.Ka.Find(k => k.innovation_number == kante.innovation_number);

            if (matchingKanteG2 == null)
            {
                if (kante.innovation_number < maxInnoG2)
                {
                    disjointGenes1.Add(kante);
                }
                else
                {
                    excessGenes.Add(kante);
                }
            }
            else
            {
                matchingGenes1.Add(kante);
            }
        }

        // Kanten von g2 klassifizieren
        foreach (var kante in g2.Ka)
        {
            if (!g1.Ka.Exists(k => k.innovation_number == kante.innovation_number))
            {
                if (kante.innovation_number < maxInnoG1)
                {
                    disjointGenes2.Add(kante);
                }
                else
                {
                    excessGenes.Add(kante);
                }
            }
            else
            {
                matchingGenes2.Add(kante);
            }
        }

        crossingGenes.Add(matchingGenes1);
        crossingGenes.Add(matchingGenes2);
        crossingGenes.Add(disjointGenes1);
        crossingGenes.Add(disjointGenes2);
        crossingGenes.Add(excessGenes);

        return crossingGenes;

    }

    public void Kreuzung(Genome g1, Genome g2, int fitterParent)
    {
        // Bestimme das fittere Genom
        Genome fitterGenome = fitterParent == 1 ? g1 : g2;
        Genome lessFitGenome = fitterParent == 1 ? g2 : g1;

        var childGenome = new GameObject("Genome_" + popOfGenome.Count + "_" + gen);
        popOfGenome.Add(childGenome);
        var g = childGenome.AddComponent<Genome>();

        childGenome.AddComponent<AgentHandler>();


        // Kopiere alle Knoten vom fitteren Elternteil
        foreach (var knoten in fitterGenome.Kn)
        {
            g.CreateNewKnoten(knoten.id, knoten.type);
        }
        // Kopieren Sie die Aktivierungsfunktionen der Knoten
        foreach (var knotenChild in childGenome.GetComponent<Genome>().Kn)
        {
            var parentKnoten = fitterParent == 1
                ? g1.Kn.Find(k => k.id == knotenChild.id)
                : g2.Kn.Find(k => k.id == knotenChild.id);

            if (parentKnoten != null)
            {
                knotenChild.activationType = parentKnoten.activationType;
            }
            else
            {
                // Wenn der Knoten neu ist, wählen Sie eine zufällige Aktivierungsfunktion
                knotenChild.activationType = (ActivationFunctions.ActivationType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ActivationFunctions.ActivationType)).Length);
            }
        }

        // Verarbeite alle Kanten
        foreach (var kante in fitterGenome.Ka)
        {
            var matchingKante = lessFitGenome.Ka.Find(k => k.innovation_number == kante.innovation_number);

            if (matchingKante != null)
            {
                // Matching Gene: Wähle zufällig von einem der Eltern
                var selectedKante = UnityEngine.Random.value < 0.5f ? kante : matchingKante;
                bool shouldActivate = selectedKante.activated || (!selectedKante.activated && UnityEngine.Random.value < pKaAC);
                g.CreateNewKante(selectedKante.in_index, selectedKante.out_index, selectedKante.weight, shouldActivate, selectedKante.innovation_number);
            }
            else
            {
                // Disjoint oder Excess Gene: Übernehme vom fitteren Elternteil
                bool shouldActivate = kante.activated || (!kante.activated && UnityEngine.Random.value < pKaAC);
                g.CreateNewKante(kante.in_index, kante.out_index, kante.weight, shouldActivate, kante.innovation_number);
            }
        }

        // Update der Knotenlayer und Spezieszuweisung
        g.GetKnotenInLayer();
        GetSpecies(g);
    }
    public void Mutation(Genome g)
    {


        // 1. Mutation eines neuen Knotens

        var randomN1 = UnityEngine.Random.Range(0f, 1f);

        if (randomN1 <= pNKn)
        {


            //Die Kante wo ein Knoten eingefügt werden soll
            var useableKanten = new List<int>();
            for (int i = 0; i < g.Ka.Count; i++)
            {
                if (g.Ka[i].activated == true)
                {
                    useableKanten.Add(i);
                }
            }
            var randomKante = UnityEngine.Random.Range(0, useableKanten.Count);
            var kante = g.Ka[useableKanten[randomKante]];

            //Ein neuer Knoten soll erstellt werden
            g.CreateNewKnoten(g.Kn[g.Kn.Count - 1].id + 1, 1);

            //Der neue Knoten
            var neuKnoten = g.Kn[g.Kn.Count - 1];

            //Kante zu neuen Knoten hin bekommt Gewicht von 1
            g.CreateNewKante(kante.in_index, neuKnoten.id, 1, true, kante.in_index * baseInnovation + neuKnoten.id);
            //Kante von neuen Knoten weg bekommt altes Gewicht
            g.CreateNewKante(neuKnoten.id, kante.out_index, kante.weight, true, neuKnoten.id * baseInnovation + kante.out_index);

            //Alte Kante ausschalten
            kante.activated = false;

            g.GetKnotenInLayer();



        }

        // 2. Mutation einer neuen Kante

        var randomN2 = UnityEngine.Random.Range(0f, 1f);

        if (randomN2 <= pNKa)
        {


            // Liste für mögliche Start- und Endknoten
            var possibleConnections = new List<(int startID, int endID, bool isDeactivated)>();

            // Finde mögliche Kanten (Startknoten darf kein Outputlayer sein, keine Self-Loops)
            foreach (var startKnoten in g.Kn)
            {
                if (startKnoten.type == 2) continue; // Überspringe Outputlayer-Knoten

                foreach (var endKnoten in g.Kn)
                {
                    // Endknoten muss in einem höheren Layer liegen als der Startknoten (Bedingung!)
                    if (startKnoten.layer < endKnoten.layer && startKnoten.id != endKnoten.id)
                    {
                        // Prüfe, ob bereits eine Kante zwischen diesen Knoten existiert
                        var existingKante = g.Ka.Find(kante => kante.in_index == startKnoten.id && kante.out_index == endKnoten.id);

                        // Wenn keine Kante existiert, oder eine existiert, aber deaktiviert ist, speichere sie
                        if (existingKante == null)
                        {
                            possibleConnections.Add((startKnoten.id, endKnoten.id, false)); // Neue Kante möglich
                        }
                        else if (!existingKante.activated)
                        {
                            possibleConnections.Add((startKnoten.id, endKnoten.id, true)); // Reaktivierbare Kante
                        }
                    }
                }
            }


            if (possibleConnections.Count > 0)
            {
                var selectedConnection = possibleConnections[UnityEngine.Random.Range(0, possibleConnections.Count)];

                if (selectedConnection.isDeactivated)
                {
                    // Reaktiviere die Kante
                    var kante = g.Ka.Find(k => k.in_index == selectedConnection.startID && k.out_index == selectedConnection.endID);
                    if (kante != null)
                    {
                        kante.activated = true;
                        kante.weight = UnityEngine.Random.Range(-0.5f, 0.5f);  // Neues zufälliges Gewicht bei Reaktivierung
                    }
                }
                else
                {
                    // Erstelle eine neue Kante
                    g.CreateNewKante(selectedConnection.startID, selectedConnection.endID, UnityEngine.Random.Range(-0.5f, 0.5f), true, selectedConnection.startID * baseInnovation + selectedConnection.endID);
                }

                g.GetKnotenInLayer();


            }
        }

        //3. Mutation des Gewichts einer Kante

        // Gewichtsmutation
        for (int i = 0; i < g.Ka.Count; i++)
        {
            if (g.Ka[i].activated && UnityEngine.Random.value < pNW)
            {
                if (UnityEngine.Random.value < 0.9f) // 90% Chance für kleine Änderung
                {
                    g.Ka[i].weight += UnityEngine.Random.Range(-0.1f, 0.1f);
                }
                else // 10% Chance für große Änderung
                {
                    g.Ka[i].weight = UnityEngine.Random.Range(-1f, 1f);
                }
                g.Ka[i].weight = Mathf.Clamp(g.Ka[i].weight, -1f, 1f);
            }
        }

        //4. Deaktivieren einer aktiven Kante

        var randomN4 = UnityEngine.Random.Range(0f, 1f);

        if (randomN4 <= pKaA)
        {
            var useableKanten = new List<int>();

            for (int i = 0; i < g.Ka.Count; i++)
            {
                if (g.Ka[i].activated == true)
                {
                    useableKanten.Add(i);
                }
            }

            var randomKantenIndex = UnityEngine.Random.Range(0, useableKanten.Count);
            var kante = g.Ka[useableKanten[randomKantenIndex]];

            kante.activated = false;
        }

        // 5. Mutation der Aktivierungsfunktion
        float randomN5 = UnityEngine.Random.Range(0f, 1f);
        if (randomN5 <= pChangeActivation)
        {
            var mutateableNodes = g.Kn.Where(n => n.type != 0 && n.type != 3).ToList(); // Keine Input- oder Bias-Knoten
            if (mutateableNodes.Count > 0)
            {
                int randomNodeIndex = UnityEngine.Random.Range(0, mutateableNodes.Count);
                Knoten nodeToMutate = mutateableNodes[randomNodeIndex];
                nodeToMutate.activationType = (ActivationFunctions.ActivationType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ActivationFunctions.ActivationType)).Length);

            }
        }

        //6.Entfernen eines Knotens (gegen zu komplexe Netzwerke)

        float randomN6 = UnityEngine.Random.Range(0f, 1f);
        if (randomN6 <= pRKn)
        {
            var removableNodes = g.Kn.Where(n => n.type == 1).ToList();
            if (removableNodes.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, removableNodes.Count);
                Knoten nodeToRemove = removableNodes[randomIndex];

                // Entferne alle Kanten, die mit diesem Knoten verbunden sind
                for (int i = g.Ka.Count - 1; i >= 0; i--)
                {
                    if (g.Ka[i].in_index == nodeToRemove.id || g.Ka[i].out_index == nodeToRemove.id)
                    {
                        DestroyImmediate(g.Ka[i].gameObject);
                        g.Ka.RemoveAt(i);
                    }
                }

                // Entferne den Knoten
                g.Kn.Remove(nodeToRemove);
                DestroyImmediate(nodeToRemove.gameObject);

                // Entferne ungültige Kanten
                g.RemoveInvalidEdges();

                // Aktualisiere die Knotenlayer
                g.GetKnotenInLayer();

                // Aktualisiere die Referenzen im Genome
                g.UpdateReferences();
            }
        }









    }

    public void TestGenomes()
    {
        creator.CreateTournament(100, 3, 4);
    }
    public void EndGeneration()
    {
        // Finde das beste Genom dieser Generation
        var bestGenomeObj = popOfGenome
            .OrderByDescending(go => go.GetComponent<Genome>().fitness)
            .First();

        var bestGenome = bestGenomeObj.GetComponent<Genome>();

        // Prüfe ob es das beste aller Zeiten ist
        if (bestGenome.fitness > allTimeBestFitness)
        {
            // Lösche altes bestes Genom falls vorhanden
            if (allTimeBestGenome != null)
            {
                Destroy(allTimeBestGenome);
            }

            // Erstelle eine vollständige Kopie des besten Genoms
            allTimeBestGenome = new GameObject("AllTimeBestGenome_Gen" + gen);
            var newGenome = allTimeBestGenome.AddComponent<Genome>();

            // Kopiere alle Knoten
            foreach (var knoten in bestGenome.Kn)
            {
                newGenome.CreateNewKnoten(knoten.id, knoten.type);
                var newKnoten = newGenome.Kn[newGenome.Kn.Count - 1];
                newKnoten.activationType = knoten.activationType;
                newKnoten.layer = knoten.layer;
            }

            // Kopiere alle Kanten
            foreach (var kante in bestGenome.Ka)
            {
                newGenome.CreateNewKante(
                    kante.in_index,
                    kante.out_index,
                    kante.weight,
                    kante.activated,
                    kante.innovation_number
                );
            }

            allTimeBestFitness = bestGenome.fitness;
            newGenome.fitness = bestGenome.fitness;
            newGenome.GetKnotenInLayer();

            Debug.Log($"Neues bestes Genom gefunden! Generation: {gen}, Fitness: {allTimeBestFitness}");
        }

        LogGenerationStats();

        
    }




    public void KillWeakestPerSpecies()
    {
        var sortedFitnessPerSpecies = new List<List<Genome>>();
        var toDestroyGenomes = new List<Genome>();

        // Gruppiere Genome nach Spezies und sortiere sie nach Fitness
        var groupedGenomes = popOfGenome.Select(go => go.GetComponent<Genome>())
                                        .GroupBy(g => g.species)
                                        .ToDictionary(g => g.Key, g => g.OrderByDescending(genome => genome.fitness).ToList());

        // Eliminiere schwächste Genome in jeder Spezies
        foreach (var speciesGroup in groupedGenomes)
        {
            int cutoff = Mathf.Max(1, Mathf.FloorToInt((1 - survivalThreshold) * speciesGroup.Value.Count));
            toDestroyGenomes.AddRange(speciesGroup.Value.Skip(cutoff));

            // Behalte die besten für das Zeichnen
            sortedFitnessPerSpecies.Add(speciesGroup.Value.Take(cutoff).ToList());
        }

        // Zeichne Phänotypen der Besten
        DrawBestPhenotypes(sortedFitnessPerSpecies);

        // Zerstöre schwache Genome
        foreach (var genome in toDestroyGenomes)
        {
            popOfGenome.Remove(genome.gameObject);
            Destroy(genome.gameObject);
        }
    }

    private void DrawBestPhenotypes(List<List<Genome>> sortedFitnessPerSpecies)
    {
        // Lösche alte Zeichnungen
       // foreach (Transform child in phenoDrawer.transform)
     //   {
      //      Destroy(child.gameObject);
      //  }

        // Zeichne die Besten jeder Spezies
        for (int i = 0; i < sortedFitnessPerSpecies.Count; i++)
        {
            if (sortedFitnessPerSpecies[i].Count > 0)
            {
               // phenoDrawer.GetComponent<Phenotype>().DrawPhenotype(sortedFitnessPerSpecies[i][0], 10 * i);
            }
        }
    } 
    public void Repopulate()
    {
        UpdateMutationRates();
        UpdateAdjustedFitness();

        gen++;
        UpdateDeltaNS();
        UpdateInnovationAges();

        var eliteCount = Mathf.Max(1, Mathf.FloorToInt(popCount * elitePerc));
        var eliteGenomes = popOfGenome
            .Select(go => go.GetComponent<Genome>())
            .OrderByDescending(g => g.fitness)
            .Take(eliteCount)
            .ToList();

        // Entferne alle nicht-Elite Genome
        foreach (var genomeObj in popOfGenome.ToList())
        {
            if (!eliteGenomes.Contains(genomeObj.GetComponent<Genome>()))
            {
                popOfGenome.Remove(genomeObj);
                Destroy(genomeObj);
            }
        }

        // Aktualisiere Spezies und entferne leere

        UpdateSpeciesList();
        AdjustSpeciesCount();

        int newGenomesNeeded = popCount - popOfGenome.Count;
        int maxAttempts = newGenomesNeeded * 2;
        int attempts = 0;
        int successfulReproductions = 0;

        while (successfulReproductions < newGenomesNeeded && attempts < maxAttempts)
        {
            attempts++;

            if (speciesList.Count == 0 || speciesList.All(s => s.members.Count == 0))
            {
                Debug.LogWarning("Keine gültigen Spezies für Reproduktion. Füge zufälliges Genom hinzu.");
                CreateRandomGenome();
                successfulReproductions++;
                continue;
            }

            Species selectedSpecies = RouletteWheelSelection(speciesList, s => s.members.Sum(g => g.adjustedFitness));

            if (selectedSpecies.members.Count < 2)
            {
                if (speciesList.Count > 1)
                {
                    continue; // Versuche es mit einer anderen Spezies
                }
                else
                {
                    // Wenn nur eine Spezies übrig ist, füge ein zufälliges Genom hinzu
                    CreateRandomGenome();
                    successfulReproductions++;
                    continue;
                }
            }

            Genome parent1 = TournamentSelection(selectedSpecies.members, 3);
            Genome parent2;

            if (UnityEngine.Random.value < interspeciesMatingRate && speciesList.Count > 1)
            {
                var otherSpecies = RouletteWheelSelection(speciesList.Where(s => s != selectedSpecies).ToList(), s => s.members.Sum(g => g.adjustedFitness));
                parent2 = TournamentSelection(otherSpecies.members, 3);
            }
            else
            {
                parent2 = TournamentSelection(selectedSpecies.members, 3);
            }

            int fitterParent = parent1.fitness > parent2.fitness ? 1 : 2;
            Kreuzung(parent1, parent2, fitterParent);
            successfulReproductions++;
        }

        if (successfulReproductions < newGenomesNeeded)
        {
            Debug.LogWarning($"Konnte nur {successfulReproductions} von {newGenomesNeeded} benötigten Genomen erzeugen. Fülle mit zufälligen Genomen auf.");
            for (int i = 0; i < newGenomesNeeded - successfulReproductions; i++)
            {
                CreateRandomGenome();
            }
        }

        // Aktualisiere Spezies für alle Genome
        foreach (var genomeObj in popOfGenome)
        {
            var genome = genomeObj.GetComponent<Genome>();
            if (genome != null)
            {
                GetSpecies(genome);
            }
        }

        // Entferne leere Spezies
        speciesList.RemoveAll(s => s.members.Count == 0);
    }

    private void UpdateSpeciesList()
    {
        foreach (var species in speciesList)
        {
            species.members.Clear();
        }

        foreach (var genomeObj in popOfGenome)
        {
            var genome = genomeObj.GetComponent<Genome>();
            if (genome != null)
            {
                var species = speciesList.Find(s => s.id == genome.species);
                if (species != null)
                {
                    species.members.Add(genome);
                }
            }
        }

        // Aktualisiere Fitness und entferne leere oder stagnierende Spezies
        var speciesToRemove = new List<Species>();
        foreach (var species in speciesList)
        {
            if (species.members.Count == 0)
            {
                speciesToRemove.Add(species);
            }
            else
            {
                float speciesBestFitness = species.members.Max(g => g.fitness);
                species.UpdateFitness(speciesBestFitness);

                if (species.generationsWithoutImprovement > 15 && species.age > spawnProtection)
                {
                    speciesToRemove.Add(species);
                }
            }
        }

        foreach (var species in speciesToRemove)
        {
            speciesList.Remove(species);
        }

        // Entferne leere Spezies
        speciesList.RemoveAll(s => s.members.Count == 0);

        if (speciesList.Count == 0)
        {
            Debug.LogWarning("Alle Spezies sind leer. Initialisiere neue Population.");
            InitializeStarterPopulation();
        }

        // Füge Stagnations-Check hinzu
        foreach (var species in speciesList.ToList())
        {
            if (species.generationsWithoutImprovement > 15)
            {
                if (species.members.Count < 5 || species.bestFitness < speciesList.Average(s => s.bestFitness))
                {
                    speciesList.Remove(species);
                }
            }
        }

        // Wenn zu wenige Spezies, senke Threshold
        if (speciesList.Count < 5)
        {
            currentCompatibilityThreshold *= 0.9f;
        }
        // Wenn zu viele Spezies, erhöhe Threshold
        else if (speciesList.Count > 20)
        {
            currentCompatibilityThreshold *= 1.1f;
        }
    }


    private T RouletteWheelSelection<T>(IEnumerable<T> items, Func<T, float> fitnessFunc)
    {
        if (items == null || !items.Any())
        {
            Debug.LogWarning("RouletteWheelSelection received an empty or null sequence.");
            return default(T);
        }

        float totalFitness = items.Sum(fitnessFunc);
        if (totalFitness <= 0)
        {
            Debug.LogWarning("Total fitness is zero or negative. Returning a random item.");
            return items.ElementAt(UnityEngine.Random.Range(0, items.Count()));
        }

        float randomPoint = UnityEngine.Random.value * totalFitness;

        foreach (var item in items)
        {
            randomPoint -= fitnessFunc(item);
            if (randomPoint <= 0)
            {
                return item;
            }
        }

        // Fallback: Wenn aus irgendeinem Grund kein Element ausgewählt wurde
        Debug.LogWarning("RouletteWheelSelection failed to select an item. Returning the last item.");
        return items.Last();
    }




    private Genome TournamentSelection(List<Genome> genomes, int tournamentSize)
    {
        Genome best = null;
        for (int i = 0; i < tournamentSize; i++)
        {
            Genome contestant = genomes[UnityEngine.Random.Range(0, genomes.Count)];
            if (best == null || contestant.fitness > best.fitness)
                best = contestant;
        }
        return best;
    }
    public void InitializeStarterPopulation()
    {
        for (int i = 0; i < popCount; i++)
        {
           // phenoDrawer.GetComponent<Phenotype>().DrawPhenotype(CreateRandomGenome(), 10 * i);
        }
    }
    private float CalculateSpeciesValue(Genome g1, Genome g2)
    {
        var crossingGenes = CrossingGenes(g1, g2);
        var numE = crossingGenes[4].Count;
        var numD = crossingGenes[3].Count + crossingGenes[2].Count;
        var m1g = crossingGenes[0];
        var m2g = crossingGenes[1];

        var numW = m1g.Count > 0 ? m1g.Zip(m2g, (a, b) => Mathf.Abs(a.weight - b.weight)).Average() : 0;

        var numN = Mathf.Max(g1.Kn.Count, g2.Kn.Count);

        // Berücksichtige geschützte Innovationen
        float protectedInnovationFactor = crossingGenes[4].Count(k => innovationAge.ContainsKey(k.innovation_number)) * 0.5f;

        return ((c1 * numE) / numN) + ((c2 * numD) / numN) + (c3 * numW) - protectedInnovationFactor;
    }
    public Genome CreateRandomGenome()
    {
        var genome = new GameObject("Genom_" + (popOfGenome.Count + 1).ToString() + "_" + gen);
        popOfGenome.Add(genome);
        var g = genome.AddComponent<Genome>();

        var agenthandler = genome.AddComponent<AgentHandler>();

        //IDS fangen bei 1 AN !!!!!!
        for (int j = 0; j < inputN; j++)
        {
            //Bias
            if (j == 0)
            {
                g.CreateNewKnoten(j + 1, 3);
            }
            else
            {
                g.CreateNewKnoten(j + 1, 0);
            }


        }
        for (int j = 0; j < outputN; j++)
        {
            g.CreateNewKnoten(inputN + j + 1, 2);
        }
        for (int j = 0; j < inputN; j++)
        {
            for (int k = 0; k < outputN; k++)
            {
                g.CreateNewKante(g.Kn[j].id, g.Kn[g.Kn.Count - k - 1].id, UnityEngine.Random.Range(-1f, 1f), true, baseInnovation * g.Kn[j].id + g.Kn[g.Kn.Count - k - 1].id);
            }
        }


        //Nicht alle Startkanten da lassen

        var kantenC = g.Ka.Count;

        for (int j = kantenC; j > percOfDeactivation * kantenC; j--)
        {
            var k = g.Ka[UnityEngine.Random.Range(0, g.Ka.Count)];

            g.Ka.Remove(k);
            Destroy(k);
        }

        g.GetKnotenInLayer();

        GetSpecies(g);

        return genome.GetComponent<Genome>();
    }
    public void UpdateAdjustedFitness()
    {
        foreach (var species in speciesList)
        {
            if (species.members.Count > 0)
            {
                float totalSpeciesFitness = species.members.Sum(g => g.fitness);
                foreach (var genome in species.members)
                {
                    genome.adjustedFitness = genome.fitness / (totalSpeciesFitness + float.Epsilon);
                }
            }
        }
    }
    private void LogGenerationStats()
    {
        var genomes = popOfGenome.Select(go => go.GetComponent<Genome>()).ToList();
        float avgFitness = genomes.Average(g => g.fitness);
        float bestFitness = genomes.Max(g => g.fitness);
        float worstFitness = genomes.Min(g => g.fitness);

        Debug.Log($"Generation {gen}:");
        Debug.Log($"Best Fitness: {bestFitness:F4}");
        Debug.Log($"Avg Fitness: {avgFitness:F4}");
        Debug.Log($"Worst Fitness: {worstFitness:F4}");
        Debug.Log($"Species Count: {speciesList.Count}");
    }
    private void UpdateMutationRates()
    {
        float diversityFactor = CalculatePopulationDiversity();
        float stagnationFactor = CalculateStagnationScore();


        // Anpassung aller Mutationsraten
        pNKn = Mathf.Lerp(baseNKn, baseNKn * 3f, 1f - diversityFactor);
        pNKa = Mathf.Lerp(baseNKa, baseNKa * 3f, 1f - diversityFactor);
        pNW = Mathf.Lerp(baseNW, 1f, stagnationFactor);

        // Knoten entfernen - erhöhen bei hoher Komplexität
        float avgNodeCount = (float)popOfGenome.Average(g => g.GetComponent<Genome>().Kn.Count);
        float complexityFactor = Mathf.Clamp01((avgNodeCount - inputN - outputN) / 20f);
        pRKn = Mathf.Lerp(baseRNKn, baseRNKn * 3f, complexityFactor);

        // Kanten deaktivieren/aktivieren
        pKaAC = Mathf.Lerp(baseKaAC, baseKaAC * 1.5f, 1f - stagnationFactor);

        // Aktivierungsfunktionen
        pChangeActivation = Mathf.Lerp(baseChangeActivation, baseChangeActivation * 2f, 1f - diversityFactor);

        // Spezies-übergreifende Kreuzung
        interspeciesMatingRate = Mathf.Lerp(0.001f, 0.05f, 1f - diversityFactor);

        // Gewichtung der Mutationen bei Stagnation
        if (stagnationFactor > 0.7f)
        {
            pNKn *= 1.5f;
            pChangeActivation *= 1.3f;
            interspeciesMatingRate *= 2f;
        }

        // Begrenzungen
        pNKn = Mathf.Clamp01(pNKn);
        pNKa = Mathf.Clamp01(pNKa);
        pNW = Mathf.Clamp01(pNW);
        pRKn = Mathf.Clamp01(pRKn);
        pKaA = Mathf.Clamp01(pKaA);
        pKaAC = Mathf.Clamp01(pKaAC);
        pChangeActivation = Mathf.Clamp01(pChangeActivation);
        interspeciesMatingRate = Mathf.Clamp01(interspeciesMatingRate);
    }


    private float CalculatePopulationDiversity()
    {
        if (popOfGenome.Count <= 1) return 1f;

        float totalDistance = 0f;
        int comparisons = 0;

        // Berechne durchschnittliche genetische Distanz
        for (int i = 0; i < popOfGenome.Count - 1; i++)
        {
            for (int j = i + 1; j < popOfGenome.Count; j++)
            {
                totalDistance += CalculateSpeciesValue(
                    popOfGenome[i].GetComponent<Genome>(),
                    popOfGenome[j].GetComponent<Genome>()
                );
                comparisons++;
            }
        }

        return totalDistance / (comparisons + float.Epsilon);
    }

    private float CalculateStagnationScore()
    {
        // Berechne wie lange keine Verbesserung stattgefunden hat
        float maxFitness = popOfGenome.Max(g => g.GetComponent<Genome>().fitness);
        float avgFitness = popOfGenome.Average(g => g.GetComponent<Genome>().fitness);

        return Mathf.Clamp01(maxFitness - avgFitness);
    }



}
