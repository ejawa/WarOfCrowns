using UnityEngine;
using System.Collections.Generic;
using WarOfCrowns.World;

namespace WarOfCrowns.Core
{
    public class Pathfinder : MonoBehaviour
    {
        public static Pathfinder Instance { get; private set; }

        private void Awake() { Instance = this; }

        private class Node
        {
            public Vector3Int Position;
            public Node Parent;
            public int G;
            public int H;
            public int F => G + H;

            public Node(Vector3Int pos) { Position = pos; }
        }

        private const int MOVE_STRAIGHT = 10;
        private const int MOVE_DIAGONAL = 14;

        public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
        {
            if (WorldGenerator.Instance == null) return null;

            Vector3Int startNode = Vector3Int.FloorToInt(startWorld);
            Vector3Int endNode = Vector3Int.FloorToInt(endWorld);

            // Если кликнули в непроходимое место, ищем ближайшую проходимую клетку
            if (!IsWalkable(endNode))
            {
                endNode = FindNearestWalkable(endNode);
                if (endNode == startNode) return null;
            }

            List<Node> openList = new List<Node>();
            HashSet<Vector3Int> closedList = new HashSet<Vector3Int>();

            Node start = new Node(startNode);
            Node target = new Node(endNode);

            openList.Add(start);

            int safetyCounter = 0;
            int maxIterations = 5000;

            while (openList.Count > 0)
            {
                safetyCounter++;
                if (safetyCounter > maxIterations) break;

                Node currentNode = openList[0];
                for (int i = 1; i < openList.Count; i++)
                {
                    if (openList[i].F < currentNode.F || (openList[i].F == currentNode.F && openList[i].H < currentNode.H))
                        currentNode = openList[i];
                }

                openList.Remove(currentNode);
                closedList.Add(currentNode.Position);

                // Если пришли
                if (currentNode.Position == target.Position)
                {
                    // --- ИЗМЕНЕНИЕ: Передаем endWorld (точную точку клика) ---
                    return SimplifyPath(RetracePath(start, currentNode, endWorld));
                    // ---------------------------------------------------------
                }

                foreach (Vector3Int neighborPos in GetNeighbors(currentNode.Position))
                {
                    if (closedList.Contains(neighborPos)) continue;

                    bool isDiagonal = (neighborPos.x != currentNode.Position.x && neighborPos.y != currentNode.Position.y);
                    int stepCost = isDiagonal ? MOVE_DIAGONAL : MOVE_STRAIGHT;
                    int terrainMultiplier = GetMovementCost(neighborPos);

                    int newCostToNeighbor = currentNode.G + (stepCost * terrainMultiplier);

                    Node neighborNode = openList.Find(n => n.Position == neighborPos);
                    if (neighborNode == null || newCostToNeighbor < neighborNode.G)
                    {
                        if (neighborNode == null)
                        {
                            neighborNode = new Node(neighborPos);
                            openList.Add(neighborNode);
                        }

                        neighborNode.G = newCostToNeighbor;
                        neighborNode.H = GetDistance(neighborPos, target.Position);
                        neighborNode.Parent = currentNode;
                    }
                }
            }

            return null;
        }

        // --- ИЗМЕНЕНИЕ: Добавлен аргумент exactEndPos ---
        private List<Vector3> RetracePath(Node startNode, Node endNode, Vector3 exactEndPos)
        {
            List<Vector3> path = new List<Vector3>();
            Node currentNode = endNode;

            // Сначала добавляем САМУЮ ПЕРВУЮ ТОЧКУ (которая будет концом пути)
            // Вместо центра клетки берем точную координату клика
            path.Add(exactEndPos);

            // Переходим к родителю, чтобы не дублировать последнюю клетку
            if (currentNode.Parent != null) currentNode = currentNode.Parent;

            while (currentNode != startNode)
            {
                // Остальные точки пути оставляем по центрам клеток
                path.Add(new Vector3(currentNode.Position.x + 0.5f, currentNode.Position.y + 0.5f, 0));
                currentNode = currentNode.Parent;
            }
            path.Reverse();
            return path;
        }

        private List<Vector3> SimplifyPath(List<Vector3> path)
        {
            if (path.Count < 3) return path;
            List<Vector3> simplifiedPath = new List<Vector3>();
            simplifiedPath.Add(path[0]);
            Vector2 lastDirection = (path[1] - path[0]).normalized;

            for (int i = 1; i < path.Count - 1; i++)
            {
                Vector2 directionNext = (path[i + 1] - path[i]).normalized;
                // Сравниваем направления с допуском, чтобы не удалять микро-повороты
                if (Vector2.Distance(directionNext, lastDirection) > 0.05f)
                {
                    simplifiedPath.Add(path[i]);
                    lastDirection = directionNext;
                }
            }
            simplifiedPath.Add(path[path.Count - 1]);
            return simplifiedPath;
        }

        private List<Vector3Int> GetNeighbors(Vector3Int node)
        {
            List<Vector3Int> neighbors = new List<Vector3Int>();

            Vector3Int r = new Vector3Int(node.x + 1, node.y, 0);
            Vector3Int l = new Vector3Int(node.x - 1, node.y, 0);
            Vector3Int u = new Vector3Int(node.x, node.y + 1, 0);
            Vector3Int d = new Vector3Int(node.x, node.y - 1, 0);

            bool walkR = IsWalkable(r);
            bool walkL = IsWalkable(l);
            bool walkU = IsWalkable(u);
            bool walkD = IsWalkable(d);

            if (walkR) neighbors.Add(r);
            if (walkL) neighbors.Add(l);
            if (walkU) neighbors.Add(u);
            if (walkD) neighbors.Add(d);

            if (walkR && walkU && IsWalkable(new Vector3Int(node.x + 1, node.y + 1, 0))) neighbors.Add(new Vector3Int(node.x + 1, node.y + 1, 0));
            if (walkR && walkD && IsWalkable(new Vector3Int(node.x + 1, node.y - 1, 0))) neighbors.Add(new Vector3Int(node.x + 1, node.y - 1, 0));
            if (walkL && walkU && IsWalkable(new Vector3Int(node.x - 1, node.y + 1, 0))) neighbors.Add(new Vector3Int(node.x - 1, node.y + 1, 0));
            if (walkL && walkD && IsWalkable(new Vector3Int(node.x - 1, node.y - 1, 0))) neighbors.Add(new Vector3Int(node.x - 1, node.y - 1, 0));

            return neighbors;
        }

        private int GetDistance(Vector3Int a, Vector3Int b)
        {
            int dstX = Mathf.Abs(a.x - b.x);
            int dstY = Mathf.Abs(a.y - b.y);
            if (dstX > dstY) return MOVE_DIAGONAL * dstY + MOVE_STRAIGHT * (dstX - dstY);
            return MOVE_DIAGONAL * dstX + MOVE_STRAIGHT * (dstY - dstX);
        }

        private int GetMovementCost(Vector3Int pos)
        {
            string biome = WorldGenerator.Instance.GetBiomeAtCell(pos);
            if (biome.Contains("Water") || biome.Contains("Sea") || biome.Contains("Ocean") || biome.Contains("Deep")) return 5;
            return 1;
        }

        private bool IsWalkable(Vector3Int pos)
        {
            string biome = WorldGenerator.Instance.GetBiomeAtCell(pos);
            if (biome.Contains("Mountain") || biome.Contains("Rock") || biome.Contains("Bedrock"))
                return false;
            return true;
        }

        private Vector3Int FindNearestWalkable(Vector3Int target)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    Vector3Int check = new Vector3Int(target.x + x, target.y + y, 0);
                    if (IsWalkable(check)) return check;
                }
            return target;
        }
    }
}