using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MathNet.Numerics.LinearAlgebra;
using System;
using Random = UnityEngine.Random;

[Serializable]
public class NNet
{
    private Matrix<float> inputLayer;

    private List<Matrix<float>> hiddenLayers = new List<Matrix<float>>();

    private Matrix<float> outputLayer = Matrix<float>.Build.Dense(1, 2);

    public List<Matrix<float>> weights = new List<Matrix<float>>();

    public float fitness;

    public void Initialise(int hiddenLayerCount, int hiddenNeuronCount, int inputs)
    {
        inputLayer = Matrix<float>.Build.Dense(1, inputs);
        
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputLayer.Clear();
        weights.Clear();

        for (int i = 0; i < hiddenLayerCount + 1; i++)
        {

            Matrix<float> f = Matrix<float>.Build.Dense(1, hiddenNeuronCount);

            hiddenLayers.Add(f);

            //WEIGHTS
            if (i == 0)
            {
                Matrix<float> inputToH1 = Matrix<float>.Build.Dense(inputLayer.ColumnCount, hiddenNeuronCount);
                weights.Add(inputToH1);
            }

            Matrix<float> HiddenToHidden = Matrix<float>.Build.Dense(hiddenNeuronCount, hiddenNeuronCount);
            weights.Add(HiddenToHidden);

        }

        Matrix<float> OutputWeight = Matrix<float>.Build.Dense(hiddenNeuronCount, 2);
        weights.Add(OutputWeight);

        RandomiseWeights();

    }

    public NNet InitialiseCopy(int hiddenLayerCount, int hiddenNeuronCount)
    {
        NNet n = new NNet();
        n.inputLayer = Matrix<float>.Build.Dense(inputLayer.RowCount, inputLayer.ColumnCount);

        List<Matrix<float>> newWeights = new List<Matrix<float>>();

        for (int i = 0; i < this.weights.Count; i++)
        {
            Matrix<float> currentWeight = Matrix<float>.Build.Dense(weights[i].RowCount, weights[i].ColumnCount);

            for (int x = 0; x < currentWeight.RowCount; x++)
            {
                for (int y = 0; y < currentWeight.ColumnCount; y++)
                {
                    currentWeight[x, y] = weights[i][x, y];
                }
            }

            newWeights.Add(currentWeight);
        }

        List<float> newBiases = new List<float>();

        n.weights = newWeights;

        n.InitialiseHidden(hiddenLayerCount, hiddenNeuronCount);

        return n;
    }

    public void InitialiseHidden(int hiddenLayerCount, int hiddenNeuronCount)
    {
        inputLayer.Clear();
        hiddenLayers.Clear();
        outputLayer.Clear();

        for (int i = 0; i < hiddenLayerCount + 1; i ++)
        {
            Matrix<float> newHiddenLayer = Matrix<float>.Build.Dense(1, hiddenNeuronCount);
            hiddenLayers.Add(newHiddenLayer);
        }

    }

    public void RandomiseWeights()
    {
        for (int i = 0; i < weights.Count; i++)
        {
            for (int x = 0; x < weights[i].RowCount; x++)
            {
                for (int y = 0; y < weights[i].ColumnCount; y++)
                {
                    weights[i][x, y] = Random.Range(-1f, 1f);
                }
            }
        }
    }

    public (float, float) RunNetwork(float[] inputs)
    {
        for (int i = 0; i < inputs.Length; i++)
        {
            inputLayer[0, i] = inputs[i];
        }

        inputLayer = inputLayer.PointwiseTanh();
        

        hiddenLayers[0] = (inputLayer * weights[0]).PointwiseTanh();

        for (int i = 1; i < hiddenLayers.Count; i++)
        {
            hiddenLayers[i] = (hiddenLayers[i - 1] * weights[i]).PointwiseTanh();
        }

        outputLayer = (hiddenLayers[^1]*weights[^1]).PointwiseTanh();

        //First output is acceleration and second output is steering
        return ((float)Math.Tanh(outputLayer[0,0]), (float)Math.Tanh(outputLayer[0,1]));
    }

    public static float Sigmoid(float s)
    {
        return (1 / (1 + Mathf.Exp(-s)));
    }

    private Matrix<float> SigmoidMatrix(Matrix<float> mat)
    {
        var temp = Matrix<float>.Build.Dense(mat.RowCount, mat.ColumnCount);
        mat.CopyTo(temp);
        
        for (int x= 0; x < temp.RowCount; x++)
        {
            for (int y = 0; y < temp.ColumnCount; y++)
            {
                temp[x, y] = Sigmoid(temp[x, y]);
            }
        }

        return temp;
    }

}
