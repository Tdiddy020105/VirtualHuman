using UnityEngine;

public class PeterPortrait : MonoBehaviour
{
    public enum EmotionState { Calm, Confused, Angry, Curious }
    public EmotionState currentEmotion = EmotionState.Calm;

    [Header("Animator Setup")]
    public Animator animator;

    void Start()
    {
        SetIdle();
    }

    public void SetEmotion(string emotion)
    {
        switch (emotion.ToLower())
        {
            case "angry": currentEmotion = EmotionState.Angry; break;
            case "confused": currentEmotion = EmotionState.Confused; break;
            case "curious": currentEmotion = EmotionState.Curious; break;
            case "calm":
            default: currentEmotion = EmotionState.Calm; break;
        }

        Debug.Log($"🧠 Emotion changed to: {currentEmotion}");
    }

    public void SetTalking()
    {
        animator?.Play("Peter_Talking");
    }

    public void SetIdle()
    {
        animator?.Play("Peter_Idle");
    }
}
