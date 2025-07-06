using System.Collections.Generic;
using UnityEngine;

public class Prey : MonoBehaviour
{
    // 静态集合存储所有猎物
    public static List<Prey> AllPrey = new List<Prey>();
    
    // 猎物状态
    public bool IsTargeted { get; set; }

    void Start()
    {
        // 加入静态列表
        AllPrey.Add(this);
        IsTargeted = false;
    }

    void OnDestroy()
    {
        // 从静态列表中移除
        AllPrey.Remove(this);
    }
}
