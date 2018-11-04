namespace TimeControl
{
    static class PluginAssemblyUtilities
    {
        internal static readonly string VERSION =
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major + "."
            + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor + "."
            + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build + "."
            + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Revision;

        internal static readonly string MOD = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;

        //internal static readonly string PathApp = KSPUtil.ApplicationRootPath.Replace( "\\", "/" );
        internal static readonly string PathPlugin = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location ).Replace( "\\", "/" );
        internal static readonly string PathPluginData = string.Format( "{0}/PluginData", PathPlugin );
        
        internal static readonly string GameDatabasePathStockToolbarIcons = string.Format( "{0}/ToolbarIcons/StockToolbarIcons", MOD );
        internal static readonly string GameDatabasePathBlizzyToolbarIcons = string.Format( "{0}/ToolbarIcons/BlizzyToolbarIcons", MOD );
        
        internal static readonly string settingsFilePath = string.Format( "{0}/settings.cfg", PathPluginData );

        //internal static readonly string PathTextures = string.Format( "{0}/Textures", PathPlugin );
        //internal static readonly string GameDatabasePathTextures = string.Format( "{0}/Textures", MOD );
        //internal static readonly string PathPluginSounds = string.Format( "{0}/Sounds", PathPlugin );

    }
}

/*
All code in this file Copyright(c) 2016 Nate West

The MIT License (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

*/
