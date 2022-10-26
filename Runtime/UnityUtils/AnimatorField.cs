using UnityEngine;

namespace SeweralIdeas.UnityUtils
{
    public static class AnimatorFieldExtensions
    {
        public static void SetValue(this Animator animator, AnimatorBool field, bool value)
        {
            field.SetValue(animator, value);
        }

        public static void SetValue(this Animator animator, AnimatorFloat field, float value)
        {
            field.SetValue(animator, value);
        }

        public static void SetValue(this Animator animator, AnimatorInt field, int value)
        {
            field.SetValue(animator, value);
        }

        public static void Trigger(this Animator animator, AnimatorTrigger field)
        {
            field.Trigger(animator);
        }

        public static void ResetTrigger(this Animator animator, AnimatorTrigger field)
        {
            field.Reset(animator);
        }
        
    }

    public struct AnimatorBool
    {
        private int m_hash;
        public AnimatorBool(string fieldName)
        {
            m_hash = Animator.StringToHash(fieldName);
        }

        public bool GetValue(Animator animator)
        {
            return animator.GetBool(m_hash);
        }

        public void SetValue(Animator animator, bool value)
        {
            animator.SetBool(m_hash, value);
        }
    }

    public struct AnimatorFloat
    {
        private int m_hash;
        public AnimatorFloat(string fieldName)
        {
            m_hash = Animator.StringToHash(fieldName);
        }

        public float GetValue(Animator animator)
        {
            return animator.GetFloat(m_hash);
        }

        public void SetValue(Animator animator, float value)
        {
            animator.SetFloat(m_hash, value);
        }
    }

    public struct AnimatorInt
    {
        private int m_hash;
        public AnimatorInt(string fieldName)
        {
            m_hash = Animator.StringToHash(fieldName);
        }

        public int GetValue(Animator animator)
        {
            return animator.GetInteger(m_hash);
        }

        public void SetValue(Animator animator, int value)
        {
            animator.SetInteger(m_hash, value);
        }
    }

    public struct AnimatorTrigger
    {
        private int m_hash;
        public AnimatorTrigger(string fieldName)
        {
            m_hash = Animator.StringToHash(fieldName);
        }

        public void Trigger(Animator animator)
        {
            animator.SetTrigger(m_hash);
        }

        public bool GetValue(Animator animator)
        {
            return animator.GetBool(m_hash);
        }

        public void Reset(Animator animator)
        {
            animator.ResetTrigger(m_hash);
        }
    }
}