﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryBuilder
{
    class DownloadErrorException : Exception
    {
        public DownloadErrorException()
        {

        }

        public DownloadErrorException(string message) : base(String.Format("Download Error: {0}", message))
        {

        }
    }
}
