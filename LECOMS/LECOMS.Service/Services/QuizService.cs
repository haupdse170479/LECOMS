using LECOMS.Data.DTOs.Course;
using LECOMS.Data.Entities;
using LECOMS.Data.Enum;
using LECOMS.RepositoryContract.Interfaces;
using LECOMS.ServiceContract.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Service.Services
{
    public class QuizService : IQuizService
    {
        private readonly IUnitOfWork _uow;

        public QuizService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<Quiz> CreateAsync(string lessonId, CreateQuizDto dto)
        {
            var lesson = await _uow.Lessons.GetAsync(l => l.Id == lessonId);
            if (lesson == null || lesson.Type != LessonType.Quiz)
                throw new InvalidOperationException("Lesson không hợp lệ.");

            var quiz = new Quiz
            {
                Id = Guid.NewGuid().ToString(),
                LessonId = lessonId,
                Questions = dto.Questions.Select(q => new QuizQuestion
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = q.Content,
                    Answers = q.Answers.Select(a => new QuizAnswer
                    {
                        Id = Guid.NewGuid().ToString(),
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };

            await _uow.Quizzes.AddAsync(quiz);
            await _uow.CompleteAsync();
            return quiz;
        }

        public async Task<bool> UpdateAsync(string quizId, CreateQuizDto dto)
        {
            using var tx = await _uow.BeginTransactionAsync();

            var quiz = await _uow.Quizzes.GetAsync(
                q => q.Id == quizId,
                includeProperties: "Questions.Answers"
            );
            if (quiz == null) return false;

            foreach (var q in quiz.Questions)
                foreach (var a in q.Answers)
                    await _uow.QuizAnswers.DeleteAsync(a);

            foreach (var q in quiz.Questions)
                await _uow.QuizQuestions.DeleteAsync(q);

            quiz.Questions = dto.Questions.Select(q => new QuizQuestion
            {
                Id = Guid.NewGuid().ToString(),
                Content = q.Content,
                Answers = q.Answers.Select(a => new QuizAnswer
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList();

            await _uow.Quizzes.UpdateAsync(quiz);
            await _uow.CompleteAsync();
            await tx.CommitAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(string quizId)
        {
            using var tx = await _uow.BeginTransactionAsync();

            var quiz = await _uow.Quizzes.GetAsync(
                q => q.Id == quizId,
                includeProperties: "Questions.Answers"
            );
            if (quiz == null) return false;

            foreach (var q in quiz.Questions)
                foreach (var a in q.Answers)
                    await _uow.QuizAnswers.DeleteAsync(a);

            foreach (var q in quiz.Questions)
                await _uow.QuizQuestions.DeleteAsync(q);

            await _uow.Quizzes.DeleteAsync(quiz);
            await _uow.CompleteAsync();
            await tx.CommitAsync();
            return true;
        }
    }

}
