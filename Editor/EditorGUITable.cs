using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SeweralIdeas.Editor
{
    public static class EditorGUITable
    {

        public delegate void ValueDrawer(Rect position, int rowId, int columnId);
        public delegate void ColumnHeaderDrawer(Rect position, int columnId);
        public delegate void RowHeaderDrawer(Rect position, int rowId);
        public delegate void CornerDrawer(Rect position);
        
        public static void TableGUI(Rect position, ref Vector2 scrollPos, List<float> columnWidths, int rowCount, int columnCount, CornerDrawer cornerDrawer, ValueDrawer valueDrawer, ColumnHeaderDrawer columnHeaderDrawer, RowHeaderDrawer rowHeaderDrawer)
        {
            float GetColumnWidth(int index, float defaultValue = 200)
            {
                if(columnWidths.Count <= index)
                    return defaultValue;
                return columnWidths[index];
            }
            
            Rect tableHeaderRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            float scrollbarHeight = GUI.skin.horizontalScrollbar.fixedHeight;
            Rect bottomPanelRect = new Rect(position.x, position.yMax - scrollbarHeight, position.width, scrollbarHeight);
            Rect tableRect = new Rect(tableHeaderRect.x, tableHeaderRect.yMax, tableHeaderRect.width, position.height - tableHeaderRect.height - bottomPanelRect.height);
            
            float innerWidth;
            
            
            // table header
            {
                int totalColumnId = 0;
                float keyWidth = GetColumnWidth(totalColumnId++);
                Rect keyHeaderRect = new Rect(tableHeaderRect.x, tableHeaderRect.y, keyWidth, tableHeaderRect.height);

                cornerDrawer(keyHeaderRect);
                Rect headerClipRect = new(tableHeaderRect.x + keyWidth, tableHeaderRect.y, tableHeaderRect.width - keyWidth, tableHeaderRect.height);
                GUI.Box(headerClipRect, "");
                GUI.BeginClip(headerClipRect, new Vector2(-scrollPos.x, 0), Vector2.zero, false);
                float innerColumnStart = 0;
                
                for(; totalColumnId <= columnCount; ++totalColumnId)
                {
                    float columnWidth = GetColumnWidth(totalColumnId);
                    Rect langHeaderRect = new Rect(innerColumnStart, 0, keyWidth, tableHeaderRect.height);
                    innerColumnStart += columnWidth;
            
                    columnHeaderDrawer(langHeaderRect, totalColumnId-1);
                }
                GUI.EndClip();
                innerWidth = keyWidth + innerColumnStart;
            }

            float rowHeight = EditorGUIUtility.singleLineHeight;

            Rect keysInnerRect = new Rect(0,0, GetColumnWidth(0), rowHeight * rowCount);
            
            scrollPos.y = GUI.BeginScrollView(tableRect, new Vector2(0, scrollPos.y), keysInnerRect).y;
            {
                // show keys
                float keysWidth = GetColumnWidth(0);
                for(int i = 0; i< rowCount; ++i)
                {
                    Rect valueRect = new Rect(0, i * rowHeight, keysWidth, rowHeight);
                    rowHeaderDrawer(valueRect, i);
                }

                // show values
                {
                    Rect valuesOuterRect = new Rect(keysWidth, 0, tableRect.width - keysWidth, keysInnerRect.height);

                    GUI.BeginClip(valuesOuterRect, new Vector2(-scrollPos.x, 0), Vector2.zero, false);

                    float columnX = 0;
                    
                    for(int columnId = 0; columnId < columnCount; ++columnId)
                    {
                        for( int rowId = 0; rowId < rowCount; ++rowId )
                        {
                            Rect valueRect = new Rect(columnX, rowId * rowHeight, keysWidth, rowHeight);
                            valueDrawer(valueRect, rowId, columnId);
                        }

                        columnX += GetColumnWidth(columnId+1);
                    }
                    GUI.EndClip();
                }
            }
            
            GUI.EndScrollView();

            // horizontal scrollBar
            float valuesOuterWidth = tableRect.width - GetColumnWidth(0);
            float valuesInnerWidth = innerWidth - GetColumnWidth(0);
            var scrollbarRect = new Rect(bottomPanelRect.x + GetColumnWidth(0), bottomPanelRect.y, valuesOuterWidth, bottomPanelRect.height);
            scrollPos.x = GUI.HorizontalScrollbar(scrollbarRect, scrollPos.x, Mathf.Min(valuesInnerWidth, valuesOuterWidth), 0f, valuesInnerWidth);
        }


    }
}
