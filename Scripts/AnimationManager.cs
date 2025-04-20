using UnityEngine;

public class AnimationManager : MonoBehaviour
{
	private Animator anim;
	private int currentStateCode = 0; // hash code of animation state

	void Start(){
		anim = gameObject.GetComponent<Animator>();
	}
    
	public void ChangeAnimationState(string newState, float offset){
		int newStateCode = Animator.StringToHash(newState);

		// this state does not exist
		if (!StateExists(newStateCode)){
			return;
		}

		// animation already playing
		if (IsPlaying(newStateCode)){
			return;
		}

		// play animation
		anim.Play(newStateCode,0,offset);

		// save new animation
		currentStateCode = newStateCode;
	}
	public void ChangeAnimationState(string newState){
		ChangeAnimationState(newState, 0); // from the start
	}

	// play an animation exactly one full time (or when passed exit time) - true when finished
	public bool PlayOnce(string newState, float exitTime = 1f){
		if (!IsPlaying(newState)){
			ChangeAnimationState(newState);
			return false;
		}
		else if (!PassedExitTime(exitTime)){ // not finished yet
			return false;
		}
        return true;
	}

	// get normalized time [0,1] representing how close the current animation is to completion (1)
	public float GetNormalizedTime(){
		AnimatorStateInfo info = anim.GetCurrentAnimatorStateInfo(0); // from layer 0
		return info.normalizedTime;
	}

	// used to simulate the 'Has Exit Time' of an animation transition
	// checks whether the current animation has player for at least exitTime (normalized)
	public bool PassedExitTime(float exitTime = 1f){
		if (GetNormalizedTime() >= exitTime){
			return true;
		}
		return false;
	}

	// does this animation state exist in the animator
	public bool StateExists(string state){
		int stateHash = Animator.StringToHash(state);
		return anim.HasState(0,stateHash);
	}
	public bool StateExists(int stateHash){
		return anim.HasState(0,stateHash);
	}

	// is this animation state currently selected (either player or finished)
	public bool IsPlaying(string state){
		int stateHash = Animator.StringToHash(state);
		if (stateHash == currentStateCode){
			return true;
		}
		return false;
	}
	public bool IsPlaying(int stateHash){
		if (stateHash == currentStateCode){
			return true;
		}
		return false;
	}
}