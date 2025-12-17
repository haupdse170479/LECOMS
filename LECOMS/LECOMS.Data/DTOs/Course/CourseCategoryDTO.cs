using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LECOMS.Data.DTOs.Course
{
    public class CourseCategoryDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }   // 👈 thêm
        public string? Description { get; set; }
    }
}
