using LECOMS.Data.DTOs.Course;
using LECOMS.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.ServiceContract.Interfaces
{
    public interface IQuizService
    {
        Task<Quiz> CreateAsync(string lessonId, CreateQuizDto dto);
        Task<bool> UpdateAsync(string quizId, CreateQuizDto dto);
        Task<bool> DeleteAsync(string quizId);
    }

}
