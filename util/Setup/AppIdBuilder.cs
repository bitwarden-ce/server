﻿using System;
using System.IO;

namespace Bit.Setup
{
    public class AppIdBuilder
    {
        private readonly Context _context;

        public AppIdBuilder(Context context)
        {
            _context = context;
        }

        public void Build()
        {
            var model = new TemplateModel
            {
                Url = _context.Config.Url
            };

            Helpers.WriteLine(_context, "Building FIDO U2F app id.");
            Directory.CreateDirectory($"{_context.DestDir}/web");
            var template = Helpers.ReadTemplate("AppId");
            using(var sw = File.CreateText($"{_context.DestDir}/web/app-id.json"))
            {
                sw.Write(template(model));
            }
        }

        public class TemplateModel
        {
            public string Url { get; set; }
        }
    }
}
