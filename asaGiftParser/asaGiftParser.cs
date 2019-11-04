using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using asaCore;

namespace asaGiftParser
{
    public class asaParser
    {
        private readonly string themeSign = "$CATEGORY: $course$/";
        private readonly ContentType jpgContentType =  new System.Net.Mime.ContentType("image/JPEG");
        private readonly ContentType pngContentType =  new System.Net.Mime.ContentType("image/png");
        private readonly string imageStart = "<img src\\=\"@@PLUGINFILE@@/";
        private readonly string imageEnd = "\" alt\\=\"\">";
        private readonly Dictionary<string, MemoryStream> _files;
        
        private asaCore.asaBuffer GetImage(string fileName)
        {
            asaCore.asaBuffer buf = null;

            if (_files.ContainsKey(fileName))
            {
                buf = new asaCore.asaBuffer {ContentType = jpgContentType};
                System.Drawing.Bitmap bm = new System.Drawing.Bitmap(_files[fileName]);
                buf.ID = Guid.NewGuid();
                buf.AutoComment = buf.ID.ToString() + ".jpg";
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                bm.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                buf.Buffer = ms.ToArray();
                buf.Version = 1;
            }
            return buf;
        }

        public asaParser(Dictionary<string, MemoryStream> files)
        {
            _files = files;
        }

        static public string GetInternalURL(asaTestUnitEx tuex, Guid BufferID)
        {
            return string.Format("[BUFER ID={0}]", BufferID);
        }
        private string PrepareString(asaTestUnitEx testUnit, string content)
        {
            content = content.Replace("\\n", "<br/>");
            int index = -1;
            while ((index = content.IndexOf(imageStart)) >= 0)
            {
                var j = content.IndexOf(imageEnd, index);
                var path = content.Substring(index + imageStart.Length, j - (index + imageStart.Length));
                var buffer = GetImage(path);
                testUnit.Buffers.Add(buffer);
                content = content.Replace($"{imageStart}{path}{imageEnd}", GetInternalURL(testUnit, buffer.ID));
                Console.WriteLine(path);
            }
            return content;
        }
        
        public Dictionary<asaTestItemTheme, IEnumerable<asaTestUnitEx>> ExtracTestUnitEx()
        {
            Dictionary<asaTestItemTheme, IEnumerable<asaTestUnitEx>> result = new Dictionary<asaTestItemTheme, IEnumerable<asaTestUnitEx>>();
            var currentTheme = asaTestItemTheme.Empty;
            ISet<asaTestUnitEx> testUnitSet = new HashSet<asaTestUnitEx>();
            if (_files.ContainsKey("gift_format.txt"))
            {
                var giftFile = _files["gift_format.txt"];
                var stringReader = new StreamReader(giftFile);
                while (!stringReader.EndOfStream)
                {
                    var line = stringReader.ReadLine();
                    if (line.StartsWith("//"))
                    {
                        continue;
                    }

                    if (line.StartsWith(themeSign))
                    {
                        if (testUnitSet.Count > 0)
                        {
                            result.Add(currentTheme, testUnitSet);
                            testUnitSet = new HashSet<asaTestUnitEx>();
                        }
                        currentTheme = new asaTestItemTheme
                        {
                            Active = true, Title = line.Remove(0, themeSign.Length), ID = Guid.NewGuid()
                        };
                        
                        continue;
                    }

                    if (line.EndsWith("{"))
                    {
                        var QuestContent = line.Remove(line.Length-1);
                        line = stringReader.ReadLine();
                        
                        if (line.StartsWith("=") && line.EndsWith("->"))
                        {
                            var question = new asaTestUnitEx
                            {
                                type = asaTestTypes.quest_type.correspondence,
                                Difficulties = 2,
                                Ball = 1,
                                ThemeID = currentTheme.ID
                            };
                            question.QuestContent = PrepareString(question, QuestContent);
                            var left = new List<string>();
                            var right = new List<string>();
                            do
                            {
                                line = line.Remove(0, 1).Remove(line.Length-2, 2);
                                left.Add(line);
                                right.Add(stringReader.ReadLine());
                                line = stringReader.ReadLine();
                            } while (line != "}");

                            for (var i = 0; i < left.Count; i++)
                            {
                                question.QuestItemList.Add(new asaQuestItem()
                                {
                                    Sequence = line.Length + i + 1,
                                    IsRight = false,
                                    Tag = 0,
                                    ItemText = PrepareString(question, left[i]),
                                    OrderNum = i + 1
                                });
                            }
                            
                            for (var i = 0; i < right.Count; i++)
                            {
                                question.QuestItemList.Add(new asaQuestItem()
                                {
                                    Sequence = 0,
                                    IsRight = false,
                                    Tag = 0,
                                    ItemText = PrepareString(question, right[i]),
                                    OrderNum = line.Length + i + 1
                                });
                            }

                            testUnitSet.Add(question);
                        }
                        else if (line.StartsWith("=") || line.StartsWith("~"))
                        {
                            var question = new asaTestUnitEx
                            {
                                type = asaTestTypes.quest_type.closed,
                                Difficulties = 2,
                                Ball = 1,
                                ThemeID = currentTheme.ID,
                            };
                            question.QuestContent = PrepareString(question, QuestContent);
                            do
                            {
                                var isRight = line.StartsWith("=");
                                line = line.Remove(0, 1);
                                question.QuestItemList.Add(new asaQuestItem()
                                {
                                    Sequence = 0,
                                    IsRight = isRight,
                                    Tag = 0,
                                    ItemText = PrepareString(question, line),
                                    OrderNum = question.QuestItemList.Count + 1
                                });
                                line = stringReader.ReadLine();
                            } while (line != "}");
                            
                            testUnitSet.Add(question);
                        }
                    }
                }
            }
            result.Add(currentTheme, testUnitSet);
            return result;
        }
    }
}