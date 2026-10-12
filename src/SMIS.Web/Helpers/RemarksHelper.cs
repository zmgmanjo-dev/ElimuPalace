namespace SMIS.Web.Helpers;

/// <summary>
/// Provides preset remarks for CBC report cards
/// </summary>
public static class RemarksHelper
{
    // ── CLASS TEACHER REMARKS ─────────────────────────────────────
    public static List<string> ClassTeacherRemarks => new()
    {
        // Excellent performance
        "An outstanding learner who demonstrates excellent mastery of all learning areas. Keep it up!",
        "A brilliant and hardworking student who consistently exceeds expectations. Very impressive performance.",
        "Exceptional performance this term. Shows great enthusiasm and commitment to learning.",
        "A top performer who demonstrates strong understanding across all subjects. Well done!",

        // Good performance
        "A good learner who has shown commendable progress this term. Keep working hard.",
        "Shows good understanding of concepts taught. With more effort, even better results are achievable.",
        "A dedicated student who participates actively in class. Good performance overall.",
        "Satisfactory performance this term. The learner shows potential for even better results.",

        // Average performance
        "The learner has made reasonable progress but needs to put in more effort to improve.",
        "Shows understanding of basic concepts. Needs to work harder on areas of weakness.",
        "A fairly good student who needs to be more consistent in classwork and assignments.",
        "The learner is making progress. More effort and focus on studies will yield better results.",

        // Needs improvement
        "The learner needs to improve their concentration and participation in class activities.",
        "Below expected performance this term. Needs extra support and encouragement at home.",
        "The learner has potential but needs to work harder. Please provide support at home.",
        "Needs significant improvement. Regular attendance and more effort in studies is advised.",

        // Attendance related
        "Frequent absenteeism has affected academic performance. Regular attendance is strongly advised.",
        "Good attendance record. The learner's commitment to school is commendable.",
    };

    // ── PRINCIPAL REMARKS ─────────────────────────────────────────
    public static List<string> PrincipalRemarks => new()
    {
        // Excellent
        "Excellent performance! You are a pride to your family and the school. Keep it up!",
        "Outstanding results! Continue with this excellent attitude towards learning.",
        "Congratulations on your superb performance. The school is proud of you.",
        "Brilliant performance! You have demonstrated exceptional academic excellence.",

        // Good
        "Good performance. Continue working hard and aim for even better results next term.",
        "Well done for your efforts this term. Keep striving for excellence.",
        "A commendable performance. With continued hard work, you will achieve even more.",
        "Good work this term. The school encourages you to maintain this momentum.",

        // Average
        "Fair performance. We encourage you to put in more effort next term.",
        "You can do better. The school believes in your potential. Work harder next term.",
        "Keep trying and never give up. With determination, better results are achievable.",
        "Average performance. More focus and dedication to studies is needed.",

        // Needs improvement
        "The school is concerned about your academic performance. Please seek help where needed.",
        "Significant improvement is needed. The school urges you to take your studies seriously.",
        "You need to work much harder. Please speak with your teacher for academic support.",
        "Performance is below expectation. Parents are encouraged to support learning at home.",

        // Promoted
        "Promoted to the next class. Best wishes as you continue your academic journey.",
        "Congratulations on your promotion. Continue to work hard in the next class.",
    };

    // ── AUTO-GENERATE REMARK BASED ON PERFORMANCE ─────────────────
    public static string GenerateTeacherRemark(string performanceSummary)
    {
        return performanceSummary switch
        {
            "EE" => ClassTeacherRemarks[0],
            "ME" => ClassTeacherRemarks[4],
            "AE" => ClassTeacherRemarks[8],
            "BE" => ClassTeacherRemarks[12],
            _ => ClassTeacherRemarks[6]
        };
    }

    public static string GeneratePrincipalRemark(string performanceSummary)
    {
        return performanceSummary switch
        {
            "EE" => PrincipalRemarks[0],
            "ME" => PrincipalRemarks[4],
            "AE" => PrincipalRemarks[8],
            "BE" => PrincipalRemarks[12],
            _ => PrincipalRemarks[6]
        };
    }
}