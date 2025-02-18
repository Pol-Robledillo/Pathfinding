using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int Size;
    public BoxCollider2D Panel;
    public GameObject token;
    //private int[,] GameMatrix; //0 not chosen, 1 player, 2 enemy de momento no hago nada con esto
    private Node[,] NodeMatrix;
    public int startPosx, startPosy;
    public int endPosx, endPosy;
    public int minObstacles = 10, maxObstacles = 15;
    void Awake()
    {
        Instance = this;
        //GameMatrix = new int[Size, Size];
        Calculs.CalculateDistances(Panel, Size);
    }
    private void Start()
    {
        /*for(int i = 0; i<Size; i++)
        {
            for (int j = 0; j< Size; j++)
            {
                GameMatrix[i, j] = 0;
            }
        }*/

        startPosx = Random.Range(0, Size);
        startPosy = Random.Range(0, Size);
        do
        {
            endPosx = Random.Range(0, Size);
            endPosy = Random.Range(0, Size);
        } while(endPosx== startPosx || endPosy== startPosy);

        //GameMatrix[startPosx, startPosy] = 2;
        //GameMatrix[startPosx, startPosy] = 1;
        NodeMatrix = new Node[Size, Size];
        CreateNodes();
        CreateObstacles();
        SearchAlgorythmAStar();
    }
    private void CreateObstacles()
    {
        int x, y;
        int obstacles = Random.Range(minObstacles, maxObstacles);
        for (int i = 0; i < obstacles; i++)
        {
            x = Random.Range(0, Size);
            y = Random.Range(0, Size);
            if (x != startPosx && y != startPosy && x != endPosx && y != endPosy)
            {
                foreach (var way in NodeMatrix[x, y].WayList)
                {
                    way.NodeDestiny.WayList.Find(waySelected => waySelected.NodeDestiny == NodeMatrix[x, y]).Cost = float.MaxValue;
                    GameObject circle = Instantiate(token, NodeMatrix[x, y].RealPosition, Quaternion.identity);
                    circle.GetComponent<SpriteRenderer>().color = Color.black;
                }
            }
            else
            {
                i--;
            }
        }
    }
    public void SearchAlgorythmAStar()
    {
        Node startNode = NodeMatrix[startPosx, startPosy];
        Node endNode = NodeMatrix[endPosx, endPosy];
        Node currentNode = startNode;

        float acumulatedCost = 0;

        Dictionary<Node, float> openList = new Dictionary<Node, float> { };
        List<Node> closedList = new List<Node>();
        openList.Add(startNode, 0);

        while (currentNode != endNode)
        {
            acumulatedCost = openList[currentNode];
            openList.Remove(currentNode);
            closedList.Add(currentNode);
            foreach (var way in currentNode.WayList)
            {
                if (!closedList.Contains(way.NodeDestiny))
                {
                    bool nodeVisited = false;
                    foreach (var node in openList)
                    {
                        if (node.Key == way.NodeDestiny)
                        {
                            nodeVisited = true;
                        }
                    }
                    if (!nodeVisited)
                    {
                        way.NodeDestiny.NodeParent = currentNode;
                        openList.Add(way.NodeDestiny, acumulatedCost + way.Cost);
                    }
                }
            }
            currentNode = openList.OrderBy(x => x.Value + x.Key.Heuristic).First().Key;
        }
        StartCoroutine(ShowResult(closedList, startNode, currentNode));
    }
    private IEnumerator ShowResult(List<Node> nodes, Node startNode, Node currentNode)
    {
        foreach (var node in nodes)
        {
            if (node != startNode)
            {
                GameObject circle = Instantiate(token, node.RealPosition, Quaternion.identity);
                circle.GetComponent<SpriteRenderer>().color = Color.yellow;
            }
            yield return new WaitForSeconds(0.5f);
        }
        StartCoroutine(ShowPath(currentNode, startNode));
    }
    private IEnumerator ShowPath(Node endNode, Node startNode)
    {
        Node currentNode = endNode.NodeParent;
        while (currentNode != startNode)
        {
            GameObject circle = Instantiate(token, currentNode.RealPosition, Quaternion.identity);
            circle.GetComponent<SpriteRenderer>().color = Color.magenta;
            currentNode = currentNode.NodeParent;
            yield return new WaitForSeconds(0.5f);
        }
    }
    public void CreateNodes()
    {
        for(int i=0; i<Size; i++)
        {
            for(int j=0; j<Size; j++)
            {
                NodeMatrix[i, j] = new Node(i, j, Calculs.CalculatePoint(i,j));
                NodeMatrix[i,j].Heuristic = Calculs.CalculateHeuristic(NodeMatrix[i,j],endPosx,endPosy);
            }
        }
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                SetWays(NodeMatrix[i, j], i, j);
            }
        }
        DebugMatrix();
    }
    public void DebugMatrix()
    {
        for (int i = 0; i < Size; i++)
        {
            for (int j = 0; j < Size; j++)
            {
                GameObject circle = Instantiate(token, NodeMatrix[i, j].RealPosition, Quaternion.identity);
                if (i == startPosx && j == startPosy)
                {
                    circle.GetComponent<SpriteRenderer>().color = Color.blue;
                }
                if (i == endPosx && j == endPosy)
                {
                    circle.GetComponent<SpriteRenderer>().color = Color.green;
                }
                circle.GetComponentsInChildren<TextMeshProUGUI>()[0].text = NodeMatrix[i, j].RealPosition.ToString();
                circle.GetComponentsInChildren<TextMeshProUGUI>()[1].text = NodeMatrix[i, j].Heuristic.ToString();
            }
        }
    }
    public void SetWays(Node node, int x, int y)
    {
        node.WayList = new List<Way>();
        if (x>0)
        {
            node.WayList.Add(new Way(NodeMatrix[x - 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if(x<Size-1)
        {
            node.WayList.Add(new Way(NodeMatrix[x + 1, y], Calculs.LinearDistance));
            if (y > 0)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y - 1], Calculs.DiagonalDistance));
            }
        }
        if(y>0)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y - 1], Calculs.LinearDistance));
        }
        if (y<Size-1)
        {
            node.WayList.Add(new Way(NodeMatrix[x, y + 1], Calculs.LinearDistance));
            if (x>0)
            {
                node.WayList.Add(new Way(NodeMatrix[x - 1, y + 1], Calculs.DiagonalDistance));
            }
            if (x<Size-1)
            {
                node.WayList.Add(new Way(NodeMatrix[x + 1, y + 1], Calculs.DiagonalDistance));
            }
        }
    }

}
