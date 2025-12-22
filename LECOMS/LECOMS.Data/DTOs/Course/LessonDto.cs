using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class LessonDto
    {
        public string Id { get; set; }
        public string CourseSectionId { get; set; }
        public string Title { get; set; }
        public LessonType Type { get; set; }
        public int? DurationSeconds { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
        public List<LessonLinkedProductDTO>? LinkedProducts { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public string? ModeratorNote { get; set; }

        // ⭐ Quiz (chỉ có khi Type = Quiz)
        public QuizDto? Quiz { get; set; }
    }
    public class QuizDto
    {
        public string Id { get; set; }
        public List<QuizQuestionDto> Questions { get; set; }
    }
    public class QuizQuestionDto
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public List<QuizAnswerDto> Answers { get; set; }
    }
    public class QuizAnswerDto
    {
        public string Id { get; set; }
        public string Content { get; set; }
    }

}
