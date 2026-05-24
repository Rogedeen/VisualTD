using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    private List<StructureManager> towers = new List<StructureManager>();
    private StructureManager mainGate;
    private List<StructureManager> allStructures = new List<StructureManager>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        
        RefreshAllLinks();
    }

    public void RefreshAllLinks()
    {
        towers.Clear();
        allStructures.Clear();
        mainGate = null;
        StructureManager[] all = Object.FindObjectsByType<StructureManager>(FindObjectsInactive.Exclude);
        foreach (var s in all)
        {
            allStructures.Add(s);
            if (s.type == StructureType.Tower) towers.Add(s);
            else if (s.type == StructureType.Gate) mainGate = s;
        }
    }

    public Transform GetDecision(Vector3 enemyPos, UnityEngine.AI.NavMeshAgent agent, out StructureManager targetStruct)
    {
        targetStruct = null;

        // 1. ÖNCELİK: GOAL YOLU AÇIK MI? (Mavi alan Goal'e kadar kesintisiz mi?)
        if (EnemyGoal.Instance != null && agent.isOnNavMesh)
        {
            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
            if (agent.CalculatePath(EnemyGoal.Instance.transform.position, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                return EnemyGoal.Instance.transform; // YOL AÇIK, KOŞ!
            }
        }

        // 2. ÖNCELİK: YOL KAPALIYSA, ULAŞILABİLİR KULE VAR MI?
        StructureManager bestTower = null;
        float minDist = Mathf.Infinity;
        foreach (var t in towers)
        {
            if (t == null || t.IsDestroyed || !t.gameObject.activeInHierarchy) continue;
            
            UnityEngine.AI.NavMeshPath path = new UnityEngine.AI.NavMeshPath();
            // Kulelerin önünde de sur olabilir, o yüzden CalculatePath ile "ulaşılabilir" olanı bul
            if (agent.CalculatePath(t.transform.position, path) && path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
            {
                float d = Vector3.Distance(enemyPos, t.transform.position);
                if (d < minDist)
                {
                    minDist = d;
                    bestTower = t;
                }
            }
        }

        if (bestTower != null)
        {
            targetStruct = bestTower;
            return bestTower.transform;
        }

        // 3. ÖNCELİK: HİÇBİR YER AÇIK DEĞİLSE, EN YAKIN DUVARA VURUP KENDİNE YAL AÇ!
        // Kapı da bir yapıdır, ama kapı yoksa bile en yakın suru (Wall) bulup yıkmalılar.
        StructureManager bestInfeasible = null;
        float minInfDist = Mathf.Infinity;

        // Tüm yapıları (Kapı + Duvar + Kule) tara, en yakınına git
        foreach (var s in allStructures)
        {
            if (s == null || s.IsDestroyed || !s.gameObject.activeInHierarchy) continue;

            float d = Vector3.Distance(enemyPos, s.transform.position);
            if (d < minInfDist)
            {
                minInfDist = d;
                bestInfeasible = s;
            }
        }

        if (bestInfeasible != null)
        {
            targetStruct = bestInfeasible;
            return bestInfeasible.transform;
        }

        return EnemyGoal.Instance?.transform;
    }

    public void RegisterStructure(StructureManager structure)
    {
        if (structure == null) return;
        if (!allStructures.Contains(structure))
        {
            allStructures.Add(structure);
            if (structure.type == StructureType.Tower)
            {
                if (!towers.Contains(structure)) towers.Add(structure);
            }
            else if (structure.type == StructureType.Gate)
            {
                mainGate = structure;
            }
        }
    }

    public void UnregisterStructure(StructureManager structure)
    {
        if (structure == null) return;
        allStructures.Remove(structure);
        if (structure.type == StructureType.Tower)
        {
            towers.Remove(structure);
        }
        else if (structure.type == StructureType.Gate)
        {
            if (mainGate == structure) mainGate = null;
        }
    }
}
