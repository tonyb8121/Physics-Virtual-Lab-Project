using UnityEngine;
using System.Threading.Tasks;

public class TeacherManager : MonoBehaviour
{
    private static TeacherManager _instance;
    public static TeacherManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TeacherManager");
                _instance = go.AddComponent<TeacherManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public async Task<bool> SubmitNewQuestion(string moduleId, string difficulty, string qText, string[] options, int correctIdx, string explanation)
    {
        if (!AuthManager.Instance.IsLoggedIn) return false;

        Question newQ = new Question
        {
            moduleId = moduleId,
            difficulty = difficulty,
            questionText = qText,
            options = options,
            correctAnswerIndex = correctIdx,
            explanation = explanation,
            teacherId = AuthManager.Instance.CurrentUser.UserId
        };

        return await FirestoreHelper.SaveCustomQuestion(newQ.teacherId, newQ);
    }
}