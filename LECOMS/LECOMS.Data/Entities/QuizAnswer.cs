using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class QuizAnswer
    {
        [Key]
        public string Id { get; set; }

        // 🔑 FK → QuizQuestion
        [Required]
        public string QuizQuestionId { get; set; }

        [ForeignKey(nameof(QuizQuestionId))]
        public QuizQuestion Question { get; set; }

        [Required]
        [MaxLength(300)]
        public string Content { get; set; }

        public bool IsCorrect { get; set; }
    }
}