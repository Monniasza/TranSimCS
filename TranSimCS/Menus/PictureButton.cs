using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MLEM.Graphics;
using MLEM.Maths;
using MLEM.Ui;
using MLEM.Ui.Elements;
using static MLEM.Ui.Elements.Image;

namespace TranSimCS.Menus {
    internal class PictureButton : Button {
        public Image Image;

        public PictureButton(Anchor anchor, Vector2 size, TextureCallback image = null, Anchor imageAnchor = Anchor.Center, Vector2? imageSize = null, Paragraph.TextCallback textCallback = null, Paragraph.TextCallback tooltipTextCallback = null) : base(anchor, size, textCallback, tooltipTextCallback) {
            CreatePicture(image, imageAnchor, imageSize ?? new(0.5f, 0.5f));
        }

        private void CreatePicture(TextureCallback image, Anchor anchor, Vector2 imageSize) {
            Image = new Image(anchor, imageSize, image);
            AddChild(Image);
        }
    }
}
