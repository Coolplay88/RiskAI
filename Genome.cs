using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class Genome : MonoBehaviour
{
    public float pOfDisabledStart;

    public int species;
    public float fitness;
    public float adjustedFitness;

    //Liste Mit Knoten
    public List<Knoten> Kn;
    //Liste mit Kanten
    public List<Kanten> Ka;

    //Anzahl an Knoten pro Schicht
    public List<int> KnPerLayer;

    public List<float> currentInput;

    public float currentOutput;

    public float expectedOutput;
    public float cumulativeError = 0;
    public int testCount = 0;

    public bool manualInputTests;

    public struct TestResult
    {
        public float Input1;
        public float Input2;
        public float ExpectedOutput;
        public float PredictedOutput;
        public float Error;
    }

    public List<TestResult> testResults = new List<TestResult>();

    public void Awake()
    {
        Kn = new List<Knoten>();
        Ka = new List<Kanten>();

        KnPerLayer = new List<int>();

        currentInput = new List<float>();

        manualInputTests = false;
    }
    public void Update()
    {
        if (manualInputTests == true)
        {
            manualInputTests = false;
            Debug.Log("Genome " + gameObject.name + " hat " + TestPredict() + " für " + currentInput[0] + " und " + currentInput[1] + " gewählt.");
        }
    }
    public void MutateWeight(int index, int value)
    {
        Ka[index].weight = value;
    }
    public void MutateActive(int index, bool a)
    {
        Ka[index].activated = a;
    }
    // Erzeugt einen neuen Knoten im Genom
    public void CreateNewKnoten(int id, int type)
    {
        var knoten = new GameObject("Knoten_" + id);
        knoten.transform.parent = transform;

        var knScript = knoten.AddComponent<Knoten>();
        knScript.type = type;
        knScript.id = id;
        knScript.g = this;
        knScript.isActive = true;


        Kn.Add(knScript);
    }


    //Erzeugt eine Neue Kante im Genom
    public void CreateNewKante(int in_i, int in_o, float w, bool a, int inno)
    {
        var kante = new GameObject("Kante_" + in_i + "_" + in_o);
        kante.transform.parent = gameObject.transform;

        var kaScript = kante.AddComponent<Kanten>();
        kaScript.in_index = in_i;
        kaScript.out_index = in_o;
        kaScript.weight = w;
        kaScript.activated = a;
        kaScript.innovation_number = inno;

        Ka.Add(kaScript);
    }
    public float Activate(float x, ActivationFunctions.ActivationType activationType)
    {
        switch (activationType)
        {
            case ActivationFunctions.ActivationType.Sigmoid:
                return 1f / (1f + Mathf.Exp(-4.9f * x));
            case ActivationFunctions.ActivationType.Tanh:
                return (float)Math.Tanh(x);
            case ActivationFunctions.ActivationType.ReLU:
                return Mathf.Max(0, x);
            case ActivationFunctions.ActivationType.LeakyReLU:
                return x > 0 ? x : 0.01f * x;
            case ActivationFunctions.ActivationType.ELU:
                return x >= 0 ? x : Mathf.Exp(x) - 1;
            default:
                return 1f / (1f + Mathf.Exp(-4.9f * x)); // Default to Sigmoid
        }
    }

    public int Predict()
    {
        float temperature = 1.0f;

        // Only calculate activations for active nodes
        for (int layer = 0; layer <= KnPerLayer.Count - 1; layer++)
        {
            foreach (var knoten in Kn.Where(k => k.layer == layer && k.isActive))
            {
                knoten.CalculateActivation();
            }
        }

        // Get only active output nodes
        var activeOutputNodes = Kn.Where(k => k.type == 2 && k.isActive).ToList();

        if (!activeOutputNodes.Any())
        {
            return 0;
        }

        // Apply softmax only on active outputs
        float[] activations = activeOutputNodes.Select(n => n.activation).ToArray();
        float[] probabilities = Softmax(activations, temperature);

        // Sample from probability distribution
        float random = UnityEngine.Random.value;
        float sum = 0;

        for (int i = 0; i < probabilities.Length; i++)
        {
            sum += probabilities[i];
            if (random <= sum)
            {
                return activeOutputNodes[i].id;
            }
        }

        return activeOutputNodes[0].id;
    }

    private float[] Softmax(float[] activations, float temperature)
    {
        float[] expValues = activations.Select(x => Mathf.Exp(x / temperature)).ToArray();
        float sum = expValues.Sum();
        return expValues.Select(x => x / sum).ToArray();
    }

    public float TestPredict()
    {
        // Berechne die Aktivierungen für alle Knoten in der richtigen Reihenfolge
        for (int layer = 0; layer <= KnPerLayer.Count - 1; layer++)
        {
            foreach (var knoten in Kn.Where(k => k.layer == layer))
            {
                knoten.CalculateActivation();
            }
        }

        // Finde den Ausgabeknoten
        var outputNode = Kn.FirstOrDefault(k => k.type == 2);
        return outputNode.activation;
    }

    public void CalculateFitness()
    {
        fitness = gameObject.transform.position.x;


    }





    //Gibt für jeden Knoten der bis zum Zeitpunkt des Methodenaufrufs erstellt wurde das Layer an und fügt es als Attribut hinzu
    public void GetKnotenInLayer()
    {

        //Resetten des KnPerLayerArrays

        KnPerLayer.Clear();

        //Output Nodes werden als letztes gemacht um letzte Schicht zu bestimmen
        var outputNeurons = new List<int>();

        //Der höchste errechnete LayerWert
        int maxLayerNumber = 0;

        for (int i = 0; i < Kn.Count; i++)
        {
            //Wenn kein OutputNeuron
            if (Kn[i].type != 2)
            {
                var layer = Kn[i].getLayer();

                Kn[i].layer = layer;

                if (layer > maxLayerNumber)
                {
                    maxLayerNumber = layer;
                }

                //Fals in theoretischen Layer, das die Liste bisher noch nicht abdeckt => aufstocken
                for (int j = KnPerLayer.Count; j - layer <= 0; j++)
                {
                    KnPerLayer.Add(0);
                }



            }
            //Wenn OutputNeuron
            else
            {
                outputNeurons.Add(i);
            }

        }


        //OutputLayer

        KnPerLayer.Add(0);

        //Setzt Layer der outputNeuronen
        for (int i = 0; i < outputNeurons.Count; i++)
        {
            Kn[outputNeurons[i]].layer = KnPerLayer.Count - 1;


        }


    }
    public float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-4.9f * x));
    }

    public void UpdateReferences()
    {
        Kn = GetComponentsInChildren<Knoten>().ToList();
        Ka = GetComponentsInChildren<Kanten>().ToList();
    }

    public void RemoveInvalidEdges()
    {
        for (int i = Ka.Count - 1; i >= 0; i--)
        {
            if (!Kn.Any(k => k.id == Ka[i].in_index) || !Kn.Any(k => k.id == Ka[i].out_index))
            {
                DestroyImmediate(Ka[i].gameObject);
                Ka.RemoveAt(i);
            }
        }
    }

}
