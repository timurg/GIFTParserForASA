using System;
using System.Collections.Generic;
using System.IO;
using asaCore;

namespace asaGiftParser
{
    public class asaParser
    {
        private readonly string themeSign = "$CATEGORY: $course$/";
        
        private readonly string imageSign = "<img src\\=\"@@PLUGINFILE@@/";
        
        private readonly Dictionary<string, MemoryStream> _files;

        public asaParser(Dictionary<string, MemoryStream> files)
        {
            _files = files;
        }

        private string PrepareString(string content)
        {
            content = content.Replace("\\n", "<br/>");
            while (content.Contains(imageSign))
            {
                
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
                                QuestContent = PrepareString(QuestContent), type = asaTestTypes.quest_type.correspondence
                            };
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
                                    ItemText = PrepareString(left[i]),
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
                                    ItemText = PrepareString(right[i]),
                                    OrderNum = line.Length + i + 1
                                });
                            }

                            testUnitSet.Add(question);
                        }
                        else if (line.StartsWith("=") || line.StartsWith("~"))
                        {
                            var question = new asaTestUnitEx
                            {
                                QuestContent = PrepareString(QuestContent), type = asaTestTypes.quest_type.closed
                            };
                            do
                            {
                                var isRight = line.StartsWith("=");
                                line = line.Remove(0, 1);
                                question.QuestItemList.Add(new asaQuestItem()
                                {
                                    Sequence = 0,
                                    IsRight = isRight,
                                    Tag = 0,
                                    ItemText = PrepareString(line),
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