using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class Quiz
    {
        [Key]
        public string Id { get; set; }

        // 🔑 FK → Lesson
        [Required]
        public string LessonId { get; set; }

        [ForeignKey(nameof(LessonId))]
        public Lesson Lesson { get; set; }

        // 1 Quiz – N Question
        public ICollection<QuizQuestion> Questions { get; set; }
    }
}
