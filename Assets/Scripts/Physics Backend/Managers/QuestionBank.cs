using UnityEngine;
using System.Collections.Generic;

// This line adds the "ARPhysicsLab" entry to your Right-Click -> Create menu
[CreateAssetMenu(fileName = "NewQuestionBank", menuName = "ARPhysicsLab/Question Bank")]
public class QuestionBank : ScriptableObject
{
    [Header("Level Info")]
    public string levelName;        // e.g., "Level 1: Foundation"
    public int levelIndex;          // 1
    public string description;      // "Form 1-2 Physics Basics"
    
    [Header("Unlock Requirements")]
    public int unlockThreshold = 70; // % score needed

    [Header("Content")]
    public List<Question> questions = new List<Question>();

    public List<Question> GetRandomBatch(int count)
    {
        List<Question> pool = new List<Question>(questions);
        List<Question> batch = new List<Question>();
        
        count = Mathf.Min(count, pool.Count);
        
        for (int i = 0; i < count; i++)
        {
            int rnd = Random.Range(0, pool.Count);
            batch.Add(pool[rnd]);
            pool.RemoveAt(rnd);
        }
        return batch;
    }
}