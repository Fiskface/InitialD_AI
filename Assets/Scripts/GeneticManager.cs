﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using Random = UnityEngine.Random;

public class GeneticManager : MonoBehaviour
{
    private CarControllerSDC controller;

    [Header("Controls")]
    public int initialPopulation = 85;
    [Range(0.0f, 1.0f)]
    public float mutationRate = 0.055f;

    [Header("Crossover Controls")] 
    public int bestAgentSelection = 8;
    public int worstAgentSelection = 3;
    public int numberToCrossover;

    private List<int> genePool = new List<int>();

    private int naturallySelected;

    private NNet[] population;
    public string filePath = "SavedNeuralNet15";

    [Header("Public View")]
    public int currentGeneration;
    public int currentGenome = 0;

    private void Awake()
    {
        controller = GetComponent<CarControllerSDC>();
    }

    private void Start()
    {
        CreatePopulation();
    }

    private void CreatePopulation()
    {
        population = new NNet[initialPopulation];
        FillPopulationWithRandomValues(population, 0);
        ResetToCurrentGenome();
    }

    private void ResetToCurrentGenome()
    {
        controller.ResetWithNetwork(population[currentGenome]);
    }

    private void FillPopulationWithRandomValues(NNet[] newPopulation, int startingIndex)
    {
        while (startingIndex < initialPopulation - 1)
        {
            newPopulation[startingIndex] = new NNet();
            newPopulation[startingIndex].Initialise(controller.LAYERS, controller.NEURONS, controller.inputs.Length);
            startingIndex++;
        }
        var saved = DeserializeFromFile<NNet>(filePath);
        if (saved != null) newPopulation[startingIndex] = saved;
        else
        {
            newPopulation[startingIndex] = new NNet();
            newPopulation[startingIndex].Initialise(controller.LAYERS, controller.NEURONS, controller.inputs.Length);
        }
    }

    public void Death(float fitness, NNet network)
    {
        network.fitness = fitness;
        
        NNet saved = DeserializeFromFile<NNet>(filePath);
        if (saved == null || saved.fitness < network.fitness)
        {
            Debug.Log($"{filePath} best fitness: {network.fitness}");
            SerializeToFile(network, filePath); 
        }
        
        if (currentGenome < population.Length -1)
        {
            currentGenome++;
            ResetToCurrentGenome();
        }
        else
        {
            RePopulate();
        }

    }

    
    private void RePopulate()
    {
        genePool.Clear();
        currentGeneration++;
        naturallySelected = 0;
        Array.Sort(population, (o1, o2) => o2.fitness.CompareTo(o1.fitness));

        NNet[] newPopulation = PickBestPopulation();

        Crossover(newPopulation);
        Mutate(newPopulation);

        FillPopulationWithRandomValues(newPopulation, naturallySelected);

        population = newPopulation;

        currentGenome = 0;

        ResetToCurrentGenome();

    }

    private void Mutate(NNet[] newPopulation)
    {
        for (int i = 0; i < naturallySelected; i++)
        {
            for (int c = 0; c < newPopulation[i].weights.Count; c++)
            {

                if (Random.Range(0.0f, 1.0f) < mutationRate)
                {
                    newPopulation[i].weights[c] = MutateMatrix(newPopulation[i].weights[c]);
                }

            }

        }

    }

    Matrix<float> MutateMatrix(Matrix<float> A)
    {

        int randomPoints = Random.Range(1, (A.RowCount * A.ColumnCount) / 7);

        Matrix<float> C = A;

        for (int i = 0; i < randomPoints; i++)
        {
            int randomColumn = Random.Range(0, C.ColumnCount);
            int randomRow = Random.Range(0, C.RowCount);

            C[randomRow, randomColumn] = Mathf.Clamp(C[randomRow, randomColumn] + Random.Range(-1f, 1f), -1f, 1f);
        }

        return C;

    }

    private void Crossover(NNet[] newPopulation)
    {
        for (int i = 0; i < numberToCrossover; i+=2)
        {
            int AIndex = i;
            int BIndex = i + 1;

            if (genePool.Count > 1)
            {
                for (int l = 0; l < 100; l++)
                {
                    AIndex = genePool[Random.Range(0, genePool.Count)];
                    BIndex = genePool[Random.Range(0, genePool.Count)];

                    if (AIndex != BIndex)
                        break;
                }
            }

            NNet Child1 = new NNet();
            NNet Child2 = new NNet();

            Child1.Initialise(controller.LAYERS, controller.NEURONS, controller.inputs.Length);
            Child2.Initialise(controller.LAYERS, controller.NEURONS, controller.inputs.Length);

            Child1.fitness = 0;
            Child2.fitness = 0;


            for (int w = 0; w < Child1.weights.Count; w++)
            {

                if (Random.Range(0.0f, 1.0f) < 0.5f)
                {
                    Child1.weights[w] = population[AIndex].weights[w];
                    Child2.weights[w] = population[BIndex].weights[w];
                }
                else
                {
                    Child2.weights[w] = population[AIndex].weights[w];
                    Child1.weights[w] = population[BIndex].weights[w];
                }

            }

            newPopulation[naturallySelected] = Child1;
            naturallySelected++;

            newPopulation[naturallySelected] = Child2;
            naturallySelected++;
        }
    }

    private NNet[] PickBestPopulation()
    {

        NNet[] newPopulation = new NNet[initialPopulation];

        for (int i = 0; i < bestAgentSelection; i++)
        {
            newPopulation[naturallySelected] = population[i].InitialiseCopy(controller.LAYERS, controller.NEURONS);
            newPopulation[naturallySelected].fitness = 0;
            naturallySelected++;
            
            int f = Mathf.RoundToInt(population[i].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(i);
            }

        }

        for (int i = 0; i < worstAgentSelection; i++)
        {
            int last = population.Length - 1;
            last -= i;

            int f = Mathf.RoundToInt(population[last].fitness * 10);

            for (int c = 0; c < f; c++)
            {
                genePool.Add(last);
            }

        }

        return newPopulation;
    }
    
    static void SerializeToFile<T>(T obj, string filePath)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(fs, obj);
        }
    }
    
    public static T DeserializeFromFile<T>(string filePath)
    {
        if (!File.Exists(filePath)) return default(T);
        using (FileStream fs = new FileStream(filePath, FileMode.Open))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return (T)formatter.Deserialize(fs);
        }
    }
}
