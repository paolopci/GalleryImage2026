using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace ImageGallery.Model
{
    public class ImageForUpdate
    {
        [Required]
        [MaxLength(150)]
        public string Title { get; set; }

        public ImageForUpdate(string title)
        {
            Title = title;
        }
    }
}
