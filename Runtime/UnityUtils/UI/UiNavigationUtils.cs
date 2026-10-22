using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace SeweralIdeas.UnityUtils.UI
{
    public interface INavigationSetter
    {
        Navigation SetNavigation(Selectable nextSelectable, Selectable previousSelectable);
    }
    
    public struct VerticalNavigationSetter : INavigationSetter
    {
        public Selectable Left;
        public Selectable Right;

        public static void SetNavigation(List<RectTransform> elements, Selectable left, Selectable right)
            => new VerticalNavigationSetter { Left = left, Right = right }.SetNavigation(elements);

        Navigation INavigationSetter.SetNavigation(Selectable next, Selectable previous) => new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnDown = next,
            selectOnUp = previous,
            selectOnLeft = Left,
            selectOnRight = Right
        };
    }

    public struct HorizontalNavigationSetter : INavigationSetter
    {
        public Selectable Up;
        public Selectable Down;

        public static void SetNavigation(List<RectTransform> elements, Selectable up, Selectable down)
            => new HorizontalNavigationSetter { Up = up, Down = down }.SetNavigation(elements);

        Navigation INavigationSetter.SetNavigation(Selectable next, Selectable previous) => new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnRight = next,
            selectOnLeft = previous,
            selectOnDown = Down,
            selectOnUp = Up
        };
    }
    
    /// <summary>
    /// Where a selectable leads in a direction, for a layout group to lead there too (see the layout groups with navigation). Null if it
    /// leads nowhere, or leads back into the group (an element of it): that would be the elements leading to each other.
    /// </summary>
    internal static class InheritedNavigation
    {
        public static Selectable Up(Selectable source, Transform group) => Filter(source ? source.FindSelectableOnUp() : null, group);
        public static Selectable Down(Selectable source, Transform group) => Filter(source ? source.FindSelectableOnDown() : null, group);
        public static Selectable Left(Selectable source, Transform group) => Filter(source ? source.FindSelectableOnLeft() : null, group);
        public static Selectable Right(Selectable source, Transform group) => Filter(source ? source.FindSelectableOnRight() : null, group);

        private static Selectable Filter(Selectable target, Transform group)
            => target && !target.transform.IsChildOf(group) ? target : null;
    }

    public static class UiNavigationUtils
    {
        private static int Wrap(this int value, int length)
        {
            if(length <= 0)
                throw new ArgumentException("Length must be greater than 0", nameof(length));
            return (value % length + length) % length;
        }

        public static void SetNavigation<TBuilder>(this TBuilder builder, List<RectTransform> elements)
            where TBuilder : struct, INavigationSetter
        {
            using (ListPool<Selectable>.Get(out var selectables))
            {
                selectables.Clear();

                foreach (RectTransform element in elements)
                {
                    Selectable selectable = element.GetComponent<Selectable>();

                    if(!selectable)
                        continue;

                    if(!selectable.gameObject.activeInHierarchy)
                        continue;
                    
                    if(!selectable.IsInteractable())
                        continue;

                    selectables.Add(selectable);
                }

                int selectablesCount = selectables.Count;

                for( int index = 0; index < selectablesCount; index++ )
                {
                    Selectable selectable = selectables[index];
                    Selectable previous = selectables[(index - 1).Wrap(selectablesCount)];
                    Selectable next = selectables[(index + 1).Wrap(selectablesCount)];
                    selectable.navigation = builder.SetNavigation(next, previous);
                }
            }
        }
    }
}