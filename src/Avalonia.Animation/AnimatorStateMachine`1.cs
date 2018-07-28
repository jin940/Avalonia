using System;
using System.Linq;
using Avalonia.Data;

namespace Avalonia.Animation
{
    /// <summary>
    /// Provides statefulness for an iteration of a keyframe animation.
    /// </summary>
    internal class AnimatorStateMachine<T> : IObservable<object>, IDisposable
    {
        T lastInterpValue;
        object firstKFValue;

        private long delayFC;
        private long durationFC;
        private ulong repeatCount;
        private long currentIteration;
        private ulong firstFrameCount;

        private bool isLooping;
        private bool isRepeating;
        private bool isReversed;
        private bool checkLoopAndRepeat;
        private bool gotFirstKFValue;
        private bool gotFirstFrameCount;

        private FillMode fillMode;
        private PlaybackDirection animationDirection;
        // private KeyFramesStates currentState;
        // private KeyFramesStates savedState;
        private Animator<T> parent;
        private Animation targetAnimation;
        private Animatable targetControl;
        private T neutralValue;
        internal bool unsubscribe = false;
        private bool isDisposed;
        private IObserver<object> targetObserver;

        public void Initialize(Animation animation, Animatable control, Animator<T> animator)
        {
            parent = animator;
            targetAnimation = animation;
            targetControl = control;
            neutralValue = (T)targetControl.GetValue(parent.Property);

            delayFC = (animation.Delay.Ticks / Timing.FrameTick.Ticks);
            durationFC = (animation.Duration.Ticks / Timing.FrameTick.Ticks);

            if (durationFC <= 0)
                throw new InvalidOperationException("Animation duration cannot be negative or zero.");

            switch (animation.RepeatCount.RepeatType)
            {
                case RepeatType.Loop:
                    isLooping = true;
                    checkLoopAndRepeat = true;
                    break;
                case RepeatType.Repeat:
                    isRepeating = true;
                    checkLoopAndRepeat = true;
                    repeatCount = animation.RepeatCount.Value;
                    break;
            }

            isReversed = (animation.PlaybackDirection & PlaybackDirection.Reverse) != 0;
            animationDirection = targetAnimation.PlaybackDirection;
            fillMode = targetAnimation.FillMode;

            // if (durationFC > 0)
            //     currentState = KeyFramesStates.DoDelay;
            // else
            //     currentState = KeyFramesStates.DoRun;
        }

        public void Step(PlayState _playState, ulong frameTick, Func<double, T, T> Interpolator)
        {
            try
            {
                InternalStep(_playState, frameTick, Interpolator);
            }
            catch (Exception e)
            {
                targetObserver?.OnError(e);
            }
        }

        private void InternalStep(PlayState playState, ulong frameTick, Func<double, T, T> Interpolator)
        {
            if (!gotFirstKFValue)
            {
                firstKFValue = parent.First().Value;
                gotFirstKFValue = true;
            }

            if (!gotFirstFrameCount)
            {
                firstFrameCount = frameTick;
                gotFirstFrameCount = true;
            }

            if (isDisposed)
                throw new InvalidProgramException("This KeyFrames Animation is already disposed.");

            // if (playState == PlayState.Stop)
            //     currentState = KeyFramesStates.Stop;

            // Save state and pause the machine
            // if (playState == PlayState.Pause && currentState != KeyFramesStates.Pause)
            // {
            //     savedState = currentState;
            //     currentState = KeyFramesStates.Pause;
            // }

            // // Resume the previous state
            // if (playState != PlayState.Pause && currentState == KeyFramesStates.Pause)
            //     currentState = savedState;

            // get the time with the initial fc as point of origin.
            var curTime = (long)(frameTick - firstFrameCount);

            // get the current iteration
            currentIteration = (long)Math.Floor((double)curTime / (double)(delayFC + durationFC));

            var isCurIterReverse = animationDirection == PlaybackDirection.Alternate ? (currentIteration % 2 == 0) ? false : true : 
                                   animationDirection == PlaybackDirection.AlternateReverse ? (currentIteration % 2 == 0) ? true : false :
                                   animationDirection == PlaybackDirection.Reverse ? true : false; 
            

            switch (currentIteration)
            {
                case 0:

                    break;
                default:

                    break;
            }

            /*
            double tempDuration = 0d, easedTime;

            bool handled = false;

            while (!handled)
            {
                ulong delayFrameCount = frameTick - firstFrameCount;
                ulong durationFrameCount = frameTick - firstFrameCount - delayTotalFrameCount;

                switch (currentState)
                {
                    case KeyFramesStates.DoDelay:

                        if (fillMode == FillMode.Backward
                         || fillMode == FillMode.Both)
                        {
                            if (currentIteration == 0)
                            {
                                _targetObserver.OnNext(firstKFValue);
                            }
                            else
                            {
                                _targetObserver.OnNext(lastInterpValue);
                            }
                        }

                        if (delayFrameCount > delayTotalFrameCount)
                        {
                            currentState = KeyFramesStates.DoRun;
                        }
                        else
                        {
                            handled = true;
                        }
                        break;

                    case KeyFramesStates.DoRun:

                        if (isReversed)
                            currentState = KeyFramesStates.RunBackwards;
                        else
                            currentState = KeyFramesStates.RunForwards;

                        break;

                    case KeyFramesStates.RunForwards:

                        if (durationFrameCount > durationTotalFrameCount)
                        {
                            currentState = KeyFramesStates.RunComplete;
                        }
                        else
                        {
                            tempDuration = (double)durationFrameCount / durationTotalFrameCount;
                            currentState = KeyFramesStates.RunApplyValue;

                        }
                        break;

                    case KeyFramesStates.RunBackwards:

                        if (durationFrameCount > durationTotalFrameCount)
                        {
                            currentState = KeyFramesStates.RunComplete;
                        }
                        else
                        {
                            tempDuration = (double)(durationTotalFrameCount - durationFrameCount) / durationTotalFrameCount;
                            currentState = KeyFramesStates.RunApplyValue;
                        }
                        break;

                    case KeyFramesStates.RunApplyValue:

                        easedTime = targetAnimation.Easing.Ease(tempDuration);
                        lastInterpValue = Interpolator(easedTime, neutralValue);
                        _targetObserver.OnNext(lastInterpValue);
                        currentState = KeyFramesStates.DoRun;
                        handled = true;
                        break;

                    case KeyFramesStates.RunComplete:

                        if (checkLoopAndRepeat)
                        {
                            firstFrameCount = frameTick;

                            if (isLooping)
                            {
                                currentState = KeyFramesStates.DoRun;
                            }
                            else if (isRepeating)
                            {
                                if (currentIteration >= repeatCount)
                                {
                                    currentState = KeyFramesStates.Stop;
                                }
                                else
                                {
                                    currentState = KeyFramesStates.DoRun;
                                }
                                currentIteration++;
                            }

                            if (animationDirection == PlaybackDirection.Alternate
                             || animationDirection == PlaybackDirection.AlternateReverse)
                                isReversed = !isReversed;

                            break;
                        }

                        currentState = KeyFramesStates.Stop;
                        break;

                    case KeyFramesStates.Stop:

                        if (fillMode == FillMode.Forward
                         || fillMode == FillMode.Both)
                        {
                            targetControl.SetValue(parent.Property, lastInterpValue, BindingPriority.LocalValue);
                        }
                        _targetObserver.OnCompleted();
                        handled = true;
                        break;
                    default:
                        handled = true;
                        break;
                }
            }
            */
        }

        public IDisposable Subscribe(IObserver<object> observer)
        {
            targetObserver = observer;
            return this;
        }

        public void Dispose()
        {
            unsubscribe = true;
            isDisposed = true;
        }
    }
}
