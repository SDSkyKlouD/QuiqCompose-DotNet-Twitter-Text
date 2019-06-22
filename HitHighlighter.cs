using System;
using System.Collections.Generic;
using System.Text;

namespace SDSK.QuiqCompose.Library.TwitterText {
    public class HitHighlighter {
        public const string DEFAULT_HIGHLIGHT_TAG = "em";

        protected string highlightTag;

        public HitHighlighter() {
            highlightTag = DEFAULT_HIGHLIGHT_TAG;
        }

        public string Highlight(string text, List<List<int>> hits) {
            if(hits == null || !(hits.Count > 0)) {
                return text;
            }

            StringBuilder sb = new StringBuilder(text.Length);
            CharEnumerator iterator = text.GetEnumerator();
            bool isCounting = true;
            bool tagOpened = false;
            int currentIndex = 0;
            char currentChar = iterator.Current;

            while(iterator.MoveNext()) {
                foreach(List<int> startEnd in hits) {
                    if(startEnd[0] == currentIndex) {
                        sb.Append(Tag(false));
                        tagOpened = true;
                    } else if(startEnd[1] == currentIndex) {
                        sb.Append(Tag(true));
                        tagOpened = false;
                    }
                }

                if(currentChar == '<') {
                    isCounting = false;
                } else if(currentChar == '>' && !isCounting) {
                    isCounting = true;
                }

                if(isCounting) {
                    currentIndex++;
                }

                sb.Append(currentChar);
                currentChar = iterator.Current;
            }

            if(tagOpened) {
                sb.Append(Tag(true));
            }

            return sb.ToString();
        }

        protected string Tag(bool closeTag) {
            StringBuilder sb = new StringBuilder(highlightTag.Length + 3);
            sb.Append("<");
            if(closeTag) {
                sb.Append("/");
            }
            sb.Append(highlightTag).Append(">");
            return sb.ToString();
        }
    }
}
