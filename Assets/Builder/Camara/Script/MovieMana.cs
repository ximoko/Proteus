using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
public class MovieMana : MonoBehaviour {
	RawImage video;
	//AudioSource au;
	// Use this for initialization
	void Start () {
		//au = this.GetComponent<AudioSource>();
		video = this.GetComponent<RawImage>();
	    }
	
	public void ReproducirVideo(MovieTexture Movie)//, AudioClip Clip)
	{
		movie = Movie;
		//clip = Clip;
		StartCoroutine("Reproducir");
	}
	MovieTexture movie;
	//AudioClip clip;	

	IEnumerator Reproducir()
	{
		video.color = new Vector4(video.color.r, video.color.g, video.color.b, 1);
		movie.Play();
		//au.clip = clip;
		//au.Play();
		yield return new WaitForSeconds(movie.duration);
		movie.Stop();
		//au.Stop();
		video.color = new Vector4(video.color.r, video.color.g, video.color.b, 1);
	}
	public MovieTexture t;
	//public AudioClip c;
	
	// Update is called once per frame
	void Update () {
        ReproducirVideo(t);//,c);		
	}
}
