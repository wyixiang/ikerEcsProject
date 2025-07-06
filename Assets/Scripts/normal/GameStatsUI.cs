using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class GameStatsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private float updateInterval = 0.5f; // 更新频率（秒）

    [Header("对象标签配置")]
    [SerializeField] private string actorTag = "Cat";
    [SerializeField] private string predatorTag = "Dog";
    [SerializeField] private string foodTag = "Food";

    private float _timer;

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= updateInterval)
        {
            UpdateStats();
            _timer = 0f;
        }
    }

    private void UpdateStats()
    {
        int actorCount = GameObject.FindGameObjectsWithTag(actorTag).Length;
        int predatorCount = GameObject.FindGameObjectsWithTag(predatorTag).Length;
        int foodCount = GameObject.FindGameObjectsWithTag(foodTag).Length;

        statsText.text = $"Cat: {actorCount} | Dog: {predatorCount} | Food: {foodCount}";
    }
}