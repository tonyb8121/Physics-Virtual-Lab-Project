using UnityEngine;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// Complete Firestore helper with all production features.
/// </summary>
public static class FirestoreHelper
{
    private static FirebaseFirestore DB => FirebaseManager.Instance.Firestore;

    #region Collections Constants
    private const string USERS = "users";
    private const string ASSIGNMENTS = "assignments";
    private const string SUBMISSIONS = "submissions";
    private const string CUSTOM_QUESTIONS = "custom_questions";
    #endregion

    #region User Data & Progress
    public static async Task SaveUserData(string userId, Dictionary<string, object> userData)
    {
        try
        {
            DocumentReference docRef = DB.Collection(USERS).Document(userId);
            await docRef.SetAsync(userData, SetOptions.MergeAll);
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to save user data: {e.Message}");
        }
    }

    public static async Task<Dictionary<string, object>> GetUserData(string userId)
    {
        try
        {
            DocumentSnapshot snapshot = await DB.Collection(USERS).Document(userId).GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ToDictionary() : null;
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to get user data: {e.Message}");
            return null;
        }
    }

    public static async Task SaveProgress(string userId, string moduleId, int score, bool completed)
    {
        try
        {
            DocumentReference progressRef = DB.Collection(USERS).Document(userId)
                                            .Collection("progress").Document(moduleId);

            var data = new Dictionary<string, object>
            {
                { "moduleId", moduleId },
                { "score", score },
                { "isCompleted", completed },
                { "lastUpdated", Timestamp.GetCurrentTimestamp() }
            };

            await progressRef.SetAsync(data, SetOptions.MergeAll);
            Logger.Log($"Progress saved: {moduleId}");
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to save progress: {e.Message}");
        }
    }

   // In FirestoreHelper.cs

public static async Task<List<Dictionary<string, object>>> GetLeaderboard(int topN = 10)
{
    try
    {
        if (FirebaseManager.Instance == null || FirebaseManager.Instance.Firestore == null)
            return new List<Dictionary<string, object>>();

        // ✅ FIX: Add .WhereEqualTo("role", "student")
        Query query = DB.Collection(USERS)
                        .WhereEqualTo("role", "student") // <--- Only show students
                        .OrderByDescending("totalPoints")
                        .Limit(topN);

        QuerySnapshot snapshot = await query.GetSnapshotAsync();
        List<Dictionary<string, object>> leaderboard = new List<Dictionary<string, object>>();

        foreach (DocumentSnapshot doc in snapshot.Documents)
        {
            if (doc.Exists)
            {
                leaderboard.Add(doc.ToDictionary());
            }
        }
        return leaderboard;
    }
    catch (Exception e)
    {
        // ⚠️ IMPORTANT: Watch the Console for an Index Link (see below)
        Logger.LogError($"Leaderboard Fetch Error: {e.Message}");
        return new List<Dictionary<string, object>>();
    }
}
// Add this inside FirestoreHelper class

    /// <summary>
    /// ✅ NEW: Adds points to the user's global score (Leaderboard).
    /// </summary>
    public static async Task AddUserPoints(string userId, int pointsToAdd)
    {
        if (pointsToAdd <= 0) return;

        try
        {
            DocumentReference userRef = DB.Collection(USERS).Document(userId);

            // Run a transaction to ensure points are added safely
            await DB.RunTransactionAsync(async transaction =>
            {
                DocumentSnapshot snapshot = await transaction.GetSnapshotAsync(userRef);
                long currentPoints = 0;

                if (snapshot.Exists && snapshot.ContainsField("totalPoints"))
                {
                    currentPoints = snapshot.GetValue<long>("totalPoints");
                }

                long newTotal = currentPoints + pointsToAdd;
                
                Dictionary<string, object> updates = new Dictionary<string, object>
                {
                    { "totalPoints", newTotal }
                };

                transaction.Set(userRef, updates, SetOptions.MergeAll);
            });

            Logger.Log($"✅ Added {pointsToAdd} points. New Total.", Color.green);
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to add points: {e.Message}");
        }
    }

    public static async Task UpdateUserName(string userId, string newName)
    {
        try
        {
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "displayName", newName }
            };
            await DB.Collection(USERS).Document(userId).UpdateAsync(updates);
        }
        catch (Exception e)
        {
            Logger.LogError($"Name Sync Error: {e.Message}");
        }
    }
    #endregion

    #region Teacher Custom Questions

    public static async Task<bool> SaveCustomQuestion(string teacherId, Question question)
    {
        try
        {
            var qData = new Dictionary<string, object>
            {
                { "teacherId", teacherId },
                { "moduleId", question.moduleId },
                { "difficulty", question.difficulty },
                { "questionText", question.questionText },
                { "options", new List<string>(question.options) },
                { "correctAnswerIndex", question.correctAnswerIndex },
                { "explanation", question.explanation },
                { "createdAt", Timestamp.GetCurrentTimestamp() }
            };

            await DB.Collection(CUSTOM_QUESTIONS).Document().SetAsync(qData);
            Logger.Log("Custom question saved successfully!", Color.green); 
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Failed to save custom question: {e.Message}");
            return false;
        }
    }

    public static async Task<List<Question>> GetTeacherQuestions(string teacherId)
    {
        try
        {
            Query query = DB.Collection(CUSTOM_QUESTIONS)
                            .WhereEqualTo("teacherId", teacherId)
                            .OrderByDescending("createdAt");

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<Question> questions = new List<Question>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                var data = doc.ToDictionary();
                List<object> optsObj = data["options"] as List<object>; 
                string[] opts = optsObj.Select(x => x.ToString()).ToArray();

                questions.Add(new Question
                {
                    id = doc.Id, 
                    questionText = data["questionText"].ToString(),
                    options = opts,
                    correctAnswerIndex = Convert.ToInt32(data["correctAnswerIndex"]),
                    explanation = data.ContainsKey("explanation") ? data["explanation"].ToString() : "",
                    moduleId = data["moduleId"].ToString(),
                    difficulty = data["difficulty"].ToString(),
                    teacherId = teacherId
                });
            }
            return questions;
        }
        catch (Exception e)
        {
            Logger.LogError($"Error fetching teacher questions: {e.Message}");
            return new List<Question>();
        }
    }

    public static async Task<bool> DeleteCustomQuestion(string questionId)
    {
        try
        {
            await DB.Collection(CUSTOM_QUESTIONS).Document(questionId).DeleteAsync();
            Logger.Log($"Question {questionId} deleted.", Color.green);
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Delete Question Error: {e.Message}");
            return false;
        }
    }
    #endregion

    #region Assignments & Grading

    public static async Task<bool> PublishAssignment(AssignmentData assignment)
    {
        try
        {
            string questionsJson = JsonConvert.SerializeObject(assignment.questions);

            var data = new Dictionary<string, object>
            {
                { "title", assignment.title },
                { "teacherId", assignment.teacherId },
                { "classId", assignment.classId },
                { "dueDate", assignment.dueDate },
                { "timeLimitMinutes", assignment.timeLimitMinutes }, // ✅ ADDED LINE
                { "createdAt", Timestamp.GetCurrentTimestamp() },
                { "questions", questionsJson }
            };

            await DB.Collection(ASSIGNMENTS).Document().SetAsync(data);
            Logger.Log($"Assignment '{assignment.title}' published!", Color.green);
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Publish Error: {e.Message}");
            // FIX: Added logging for Inner Exception to debug "Publish Failed"
            if (e.InnerException != null) Logger.LogError($"Inner Error: {e.InnerException.Message}");
            return false;
        }
    }

    public static async Task<bool> HasAdmNoSubmitted(string assignmentId, string admNo)
    {
        try
        {
            Query query = DB.Collection(SUBMISSIONS)
                            .WhereEqualTo("assignmentId", assignmentId)
                            .WhereEqualTo("admissionNumber", admNo) 
                            .Limit(1);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count > 0;
        }
        catch (Exception e) 
        { 
            Logger.LogError($"AdmNo Check Error: {e.Message}");
            return false;
        } 
    }

    public static async Task<string> SubmitGrade(StudentSubmission submission)
    {
        try
        {
            bool alreadyDone = await HasAdmNoSubmitted(submission.assignmentId, submission.admissionNumber);
            if (alreadyDone)
            {
                Logger.LogError("Cheating Prevention: This Admission Number already submitted.");
                return "Duplicate";
            }

            var data = new Dictionary<string, object>
            {
                { "assignmentId", submission.assignmentId },
                { "studentId", submission.studentId },
                { "studentName", submission.studentName },
                { "admissionNumber", submission.admissionNumber },
                { "score", submission.score },
                { "totalQuestions", submission.totalQuestions },
                { "submittedAt", Timestamp.GetCurrentTimestamp() }
            };

            string docId = $"{submission.assignmentId}_{submission.studentId}";
            await DB.Collection(SUBMISSIONS).Document(docId).SetAsync(data);
            Logger.Log($"Submission recorded for {submission.studentName}.");
            return "Success";
        }
        catch (Exception e)
        {
            Logger.LogError($"Submit Error: {e.Message}");
            return "Error";
        }
    }

    public static async Task<List<StudentSubmission>> GetAssignmentGrades(string assignmentId)
    {
        try
        {
            Query query = DB.Collection(SUBMISSIONS)
                                 .WhereEqualTo("assignmentId", assignmentId)
                                 .OrderByDescending("score");

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<StudentSubmission> grades = new List<StudentSubmission>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                Timestamp submittedTimestamp = dict.ContainsKey("submittedAt") ? (Timestamp)dict["submittedAt"] : default(Timestamp);
                
                int totalQs = Convert.ToInt32(dict["totalQuestions"]);
                int score = Convert.ToInt32(dict["score"]);

                grades.Add(new StudentSubmission
                {
                    id = doc.Id,
                    assignmentId = assignmentId,
                    studentId = dict["studentId"].ToString(),
                    studentName = dict["studentName"].ToString(),
                    admissionNumber = dict.ContainsKey("admissionNumber") ? dict["admissionNumber"].ToString() : "N/A", 
                    score = score,
                    totalQuestions = totalQs,
                    submittedAt = submittedTimestamp.ToDateTime().Ticks,
                    percentage = (totalQs > 0) ? ((float)score / totalQs) * 100f : 0f
                });
            }
            return grades;
        }
        catch (Exception e)
        {
            Logger.LogError($"Fetch Grades Error: {e.Message}");
            return new List<StudentSubmission>();
        }
    }

    public static async Task<List<StudentSubmission>> GetSubmissionsByAssignment(string assignmentId)
    {
        try
        {
            Query query = DB.Collection(SUBMISSIONS).WhereEqualTo("assignmentId", assignmentId);
            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<StudentSubmission> submissions = new List<StudentSubmission>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                Timestamp submittedTimestamp = dict.ContainsKey("submittedAt") ? (Timestamp)dict["submittedAt"] : default(Timestamp);
                
                int totalQs = Convert.ToInt32(dict["totalQuestions"]);
                int score = Convert.ToInt32(dict["score"]);

                submissions.Add(new StudentSubmission
                {
                    id = doc.Id,
                    assignmentId = assignmentId,
                    studentId = dict["studentId"].ToString(),
                    studentName = dict["studentName"].ToString(),
                    admissionNumber = dict.ContainsKey("admissionNumber") ? dict["admissionNumber"].ToString() : "N/A", 
                    score = score,
                    totalQuestions = totalQs,
                    submittedAt = submittedTimestamp.ToDateTime().Ticks,
                    percentage = (totalQs > 0) ? ((float)score / totalQs) * 100f : 0f
                });
            }
            return submissions;
        }
        catch (Exception e)
        {
            Logger.LogError($"Fetch Submissions Error: {e.Message}");
            return new List<StudentSubmission>();
        }
    }

    /// <summary>
    /// ✅ NEW: Get analytics for a specific assignment (teacher view).
    /// Returns: { avgScore, completionRate, totalSubmissions }
    /// </summary>
    public static async Task<AssignmentAnalytics> GetAssignmentAnalytics(string assignmentId, string classId)
    {
        try
        {
            // Get all submissions for this assignment
            var submissions = await GetSubmissionsByAssignment(assignmentId);
            
            // Get total students in this class
            Query classQuery = DB.Collection(USERS)
                                 .WhereEqualTo("classId", classId)
                                 .WhereEqualTo("role", "student");
            var classSnapshot = await classQuery.GetSnapshotAsync();
            int totalStudents = classSnapshot.Count;

            if (submissions.Count == 0)
            {
                return new AssignmentAnalytics
                {
                    averageScore = 0,
                    completionRate = 0,
                    totalSubmissions = 0,
                    totalStudents = totalStudents
                };
            }

            // Calculate average score
            float avgScore = submissions.Average(s => s.percentage);
            float completionRate = totalStudents > 0 ? (float)submissions.Count / totalStudents * 100f : 0;

            return new AssignmentAnalytics
            {
                averageScore = avgScore,
                completionRate = completionRate,
                totalSubmissions = submissions.Count,
                totalStudents = totalStudents
            };
        }
        catch (Exception e)
        {
            Logger.LogError($"Analytics Error: {e.Message}");
            return new AssignmentAnalytics();
        }
    }

    public static async Task<List<AssignmentData>> GetAssignments(string classId)
    {
        try
        {
            Query query = DB.Collection(ASSIGNMENTS)
                            .WhereEqualTo("classId", classId)
                            .OrderByDescending("createdAt");

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<AssignmentData> list = new List<AssignmentData>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                
                string questionsJson = dict["questions"].ToString();
                List<Question> questions = JsonConvert.DeserializeObject<List<Question>>(questionsJson); 

                long dueDate = ParseTimestamp(dict, "dueDate");
                long createdAt = ParseTimestamp(dict, "createdAt");

                AssignmentData a = new AssignmentData
                {
                    id = doc.Id,
                    title = dict["title"].ToString(),
                    teacherId = dict["teacherId"].ToString(),
                    classId = dict["classId"].ToString(),
                    dueDate = dueDate,
                    timeLimitMinutes = dict.ContainsKey("timeLimitMinutes") ? Convert.ToInt32(dict["timeLimitMinutes"]) : 0, // ✅ ADDED LINE
                    createdAt = createdAt,
                    questions = questions,
                    isOverdue = dueDate < DateTime.Now.Ticks 
                };
                list.Add(a);
            }
            return list;
        }
        catch (Exception e)
        {
            Logger.LogError($"Fetch Assignments Error: {e.Message}");
            return new List<AssignmentData>();
        }
    }

    public static async Task<List<StudentSubmission>> GetStudentSubmissionHistory(string studentId)
    {
        try
        {
            Query query = DB.Collection(SUBMISSIONS)
                            .WhereEqualTo("studentId", studentId)
                            .OrderByDescending("submittedAt");

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<StudentSubmission> history = new List<StudentSubmission>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();
                Timestamp submittedTimestamp = dict.ContainsKey("submittedAt") ? (Timestamp)dict["submittedAt"] : default(Timestamp);
                
                int totalQs = Convert.ToInt32(dict["totalQuestions"]);
                int score = Convert.ToInt32(dict["score"]);

                history.Add(new StudentSubmission
                {
                    id = doc.Id,
                    assignmentId = dict["assignmentId"].ToString(),
                    studentId = dict["studentId"].ToString(),
                    studentName = dict["studentName"].ToString(),
                    admissionNumber = dict.ContainsKey("admissionNumber") ? dict["admissionNumber"].ToString() : "N/A",
                    score = score,
                    totalQuestions = totalQs,
                    submittedAt = submittedTimestamp.ToDateTime().Ticks,
                    percentage = (totalQs > 0) ? ((float)score / totalQs) * 100f : 0f
                });
            }
            return history;
        }
        catch (Exception e)
        {
            Logger.LogError($"Fetch History Error: {e.Message}");
            return new List<StudentSubmission>();
        }
    }

    public static async Task<bool> JoinClass(string userId, string classCode, string admissionNumber)
    {
        try
        {
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "classId", classCode },
                { "admissionNumber", admissionNumber }
            };
            await DB.Collection(USERS).Document(userId).UpdateAsync(updates);
            
            Logger.Log($"User {userId} joined {classCode} with AdmNo {admissionNumber}");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Join Class Error: {e.Message}");
            return false;
        }
    }

    public static async Task<List<AssignmentData>> GetAssignmentsByTeacher(string teacherId)
    {
        try
        {
            Query query = DB.Collection(ASSIGNMENTS)
                            .WhereEqualTo("teacherId", teacherId)
                            .OrderByDescending("createdAt");

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            List<AssignmentData> assignments = new List<AssignmentData>();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                var dict = doc.ToDictionary();

                string questionsJson = dict["questions"].ToString();
                List<Question> questions = JsonConvert.DeserializeObject<List<Question>>(questionsJson);

                long dueDate = ParseTimestamp(dict, "dueDate");
                long createdAt = ParseTimestamp(dict, "createdAt");

                AssignmentData a = new AssignmentData
                {
                    id = doc.Id,
                    title = dict["title"].ToString(),
                    teacherId = dict["teacherId"].ToString(),
                    classId = dict["classId"].ToString(),
                    dueDate = dueDate,
                    timeLimitMinutes = dict.ContainsKey("timeLimitMinutes") ? Convert.ToInt32(dict["timeLimitMinutes"]) : 0,
                    createdAt = createdAt,
                    questions = questions,
                    isOverdue = dueDate < DateTime.Now.Ticks
                };
                assignments.Add(a);
            }
            return assignments;
        }
        catch (Exception e)
        {
            Logger.LogError($"Fetch Teacher Assignments Error: {e.Message}");
            return new List<AssignmentData>();
        }
    }

    public static async Task<bool> DeleteAssignment(string assignmentId)
    {
        try
        {
            await DB.Collection(ASSIGNMENTS).Document(assignmentId).DeleteAsync();
            var submissions = await GetSubmissionsByAssignment(assignmentId);
            
            foreach (var sub in submissions)
            {
                string docId = $"{sub.assignmentId}_{sub.studentId}"; 
                await DB.Collection(SUBMISSIONS).Document(docId).DeleteAsync();
            }
            Logger.Log($"Assignment {assignmentId} deleted.");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Delete Error: {e.Message}");
            return false;
        }
    }

    public static async Task<bool> VerifyClassCode(string classCode)
    {
        try
        {
            Query query = DB.Collection(ASSIGNMENTS)
                            .WhereEqualTo("classId", classCode)
                            .Limit(1);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();
            return snapshot.Count > 0;
        }
        catch (Exception e)
        {
            Logger.LogError($"Verification Error: {e.Message}");
            return false;
        }
    }

    public static async Task<bool> UpdateAssignmentDueDate(string assignmentId, long newDateTicks)
    {
        try
        {
            Dictionary<string, object> updates = new Dictionary<string, object>
            {
                { "dueDate", newDateTicks }
            };
            await DB.Collection(ASSIGNMENTS).Document(assignmentId).UpdateAsync(updates);
            Logger.Log("Due date updated.", Color.green);
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError($"Update Failed: {e.Message}");
            return false;
        }
    }

    public static async Task<Dictionary<string, object>> GetSchoolDetails(string schoolId)
    {
        try 
        {
            var doc = await DB.Collection("schools").Document(schoolId).GetSnapshotAsync();
            return doc.Exists ? doc.ToDictionary() : null;
        }
        catch (Exception e)
        {
            Logger.LogError($"Get School Error: {e.Message}");
            return null;
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Consistent timestamp parsing from Firestore documents.
    /// </summary>
    private static long ParseTimestamp(Dictionary<string, object> dict, string key)
    {
        if (!dict.ContainsKey(key)) return DateTime.Now.AddDays(7).Ticks;
        
        object value = dict[key];
        if (value is long l) return l;
        if (value is Timestamp ts) return ts.ToDateTime().Ticks;
        
        return DateTime.Now.AddDays(7).Ticks;
    }
    #endregion
}

// ========== NEW DATA STRUCTURES (Retained from previous step) ==========

/// <summary>
/// ✅ NEW: Analytics data for teacher dashboard.
/// </summary>
[System.Serializable]
public class AssignmentAnalytics
{
    public float averageScore;      // Average percentage
    public float completionRate;    // % of students who submitted
    public int totalSubmissions;    // Number of submissions
    public int totalStudents;       // Total students in class
}