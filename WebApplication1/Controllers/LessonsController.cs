using Microsoft.AspNetCore.Mvc;
using WebRTC.Models;

namespace WebRTC.Controllers
{
    public class LessonsController : Controller
    {
        private static readonly List<Lesson> lessons = new List<Lesson>
        {
            new Lesson { Id = Guid.NewGuid(), Name = "Türkçe" },
            new Lesson { Id = Guid.NewGuid(), Name = "Matematik" },
            new Lesson { Id = Guid.NewGuid(), Name = "Kimya" }
        };

        public IActionResult Index()
        {
            return View(lessons);
        }

        [HttpGet("class/{Id:guid}")]
        public IActionResult Class([FromRoute] Guid Id)
        {
            var lesson = lessons.FirstOrDefault(x => x.Id == Id);
            if (lesson == null)
            {
                return NotFound();
            }

            return View(lesson);
        }
    }
}