using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.Entities
{
    public class QuizQuestion
    {
        [Key]
        public string Id { get; set; }

        // 🔑 FK → Quiz
        [Required]
        public string QuizId { get; set; }

        [ForeignKey(nameof(QuizId))]
        public Quiz Quiz { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; }

        // 1 Question – N Answer
        public ICollection<QuizAnswer> Answers { get; set; }
    }
}
