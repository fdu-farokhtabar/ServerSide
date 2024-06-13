using System;

namespace Domain.Entity
{
    public class Page
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string MetaKeywords { get; set; }
        public string MetaDescription { get; set; }
        /// <summary>
        /// {Row [Fullscreen] {Slider [Alt,Link][Alt,Link][Alt,Link]}{Grid[ColsPerRow,BoxTheme,TitleDirection] [Title,Link][Title,Link]}{Text [content,direction,FontSize,Bold][content,direction,FontSize,Bold]}}
        /// </summary>
        public string Content { get; set; }
    }
}
