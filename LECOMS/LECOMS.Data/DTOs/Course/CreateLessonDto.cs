using LECOMS.Data.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class CreateLessonDto
    {
        public string CourseSectionId { get; set; }
        public string Title { get; set; }
        public LessonType Type { get; set; }
        public int? DurationSeconds { get; set; }
        public string? ContentUrl { get; set; }
        public int OrderIndex { get; set; }
        // ⭐ Quiz (optional – chỉ dùng khi Type = Quiz)
        public CreateQuizDto? Quiz { get; set; }
    }

    public class CreateQuizDto
    {
        public List<CreateQuizQuestionDto> Questions { get; set; }
    }
    public class CreateQuizQuestionDto
    {
        public string Content { get; set; }
        public List<CreateQuizAnswerDto> Answers { get; set; }
    }
    public class CreateQuizAnswerDto
    {
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }

}
