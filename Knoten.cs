using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Knoten : MonoBehaviour
{
    public Genome g;
    public int id;
    //0 für Input ; 1 für Hidden ; 2 für Output; 3 für bias
    public int type;

    public int layer;

    public float activation;

    public ActivationFunctions.ActivationType activationType = ActivationFunctions.ActivationType.Sigmoid;

    public bool isActive;



    //Gibt den Layer aus auf dem Sich der Knoten aus geometrischer Sicht befindet
    public int getLayer()
    {
        // Wenn Input, dann Layer 0
        if (type == 0 || type == 3)
        {
            return 0;
        }
        else
        {
            // Liste nur erstellen, wenn nötig
            List<Knoten> searchKnoten = null;

            for (int i = 0; i < g.Ka.Count; i++)
            {
                if (g.Ka[i].out_index == id)
                {
                    var searchedIndex = g.Ka[i].in_index;

                    // Direkt den Knoten anhand des Index finden
                    for (int j = 0; j < g.Kn.Count; j++)
                    {
                        if (g.Kn[j].id == searchedIndex)
                        {
                            if (searchKnoten == null)
                            {
                                searchKnoten = new List<Knoten>();
                            }
                            searchKnoten.Add(g.Kn[j]);
                            break; // Da wir den Knoten gefunden haben, die Schleife abbrechen
                        }
                    }
                }
            }

            // Berechne den maximalen Layer
            int maxLayer = 0;

            if (searchKnoten != null && searchKnoten.Count > 0)
            {
                foreach (Knoten knoten in searchKnoten)
                {
                    maxLayer = Mathf.Max(maxLayer, 1 + knoten.getLayer());
                }
            }

            // Gib den maximalen Layer zurück
            return maxLayer;
        }
    }

    public void CalculateActivation()
    {
        if (type == 0 || type == 3) // Eingangs- oder Bias-Knoten
        {
            if (id - 1 < g.currentInput.Count)
            {
                activation = g.currentInput[id - 1];
            }
            else
            {
                Debug.LogWarning($"Input index out of range for node {id}");
                activation = 0;
            }
        }
        else
        {
            float sum = 0f;
            foreach (var kante in g.Ka)
            {
                if (kante.out_index == id && kante.activated)
                {
                    Knoten inputKnoten = g.Kn.FirstOrDefault(k => k.id == kante.in_index);
                    if (inputKnoten != null)
                    {
                        sum += inputKnoten.activation * kante.weight;
                    }
                    else
                    {
                        Debug.LogWarning($"Input node {kante.in_index} not found for edge to node {id}");
                    }
                }
            }
            activation = g.Activate(sum, activationType);
        }
    }



}
