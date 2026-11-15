$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
public static class MainMenuPngInspection {
 public static string[] Run(string root, string output) {
  var files=Directory.GetFiles(root,"*.png",SearchOption.AllDirectories);Array.Sort(files);
  var reports=new List<string>();var sheet=new Bitmap(1500,Math.Max(1,(files.Length+4)/5)*230);
  using(var g=Graphics.FromImage(sheet))using(var font=new Font("Arial",11)) {
   g.Clear(Color.FromArgb(12,28,45));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
   for(int n=0;n<files.Length;n++)using(var b=new Bitmap(files[n])) {
    int left=b.Width,top=b.Height,right=-1,bottom=-1,transparent=0,partial=0;
    for(int y=0;y<b.Height;y++)for(int x=0;x<b.Width;x++){int a=b.GetPixel(x,y).A;if(a==0)transparent++;else{if(a<255)partial++;left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}}
    string report=Path.GetFileName(files[n])+" "+b.Width+"x"+b.Height+" transparent="+transparent+" partial="+partial+" alphaBounds="+left+","+top+","+(right-left+1)+","+(bottom-top+1);
    reports.Add(report);int px=(n%5)*300,py=(n/5)*230;
    for(int y=0;y<180;y+=12)for(int x=0;x<288;x+=12)using(var brush=new SolidBrush((x/12+y/12)%2==0?Color.FromArgb(35,51,68):Color.FromArgb(48,64,79)))g.FillRectangle(brush,px+x,py+y,12,12);
    float scale=Math.Min(282f/b.Width,174f/b.Height);float w=b.Width*scale,h=b.Height*scale;
    g.DrawImage(b,px+(288-w)/2,py+(180-h)/2,w,h);g.DrawString(Path.GetFileName(files[n]),font,Brushes.White,px+3,py+184);g.DrawString(b.Width+" x "+b.Height+(transparent==0?"  OPAQUE":"  ALPHA"),font,Brushes.LightCyan,px+3,py+205);
   }
  }
  Directory.CreateDirectory(output);sheet.Save(Path.Combine(output,"Assets-contact-sheet.png"),ImageFormat.Png);sheet.Dispose();File.WriteAllLines(Path.Combine(output,"PNG-inspection.txt"),reports);return reports.ToArray();
 }
}
'@
[MainMenuPngInspection]::Run((Join-Path $PWD 'Assets/Art/UI/MainMenu'), (Join-Path $PWD 'Logs/MainMenu'))
