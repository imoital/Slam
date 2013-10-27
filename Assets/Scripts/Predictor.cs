using UnityEngine;
using System.Collections;

public class Predictor {
	
	private float client_ping;
	private NetState[] server_state_buffer = new NetState[20];
	public float position_error_threshold = 0.2f;
	
	public float PING_MARGIN = 0.5f;
	
	public Transform observed_transform;
	
	public Vector3 server_pos;
	
	public Predictor(Transform transform)
	{
		observed_transform = transform;
	}
	
	public void LerpToTarget()
	{
		float distance = Vector3.Distance(observed_transform.position, server_pos);
		
		// only correct if the error margin (the distance) is too extreme
		if (distance >= position_error_threshold) {
			
			float lerp = ((1 / distance) * 10f) / 100f;
			
			observed_transform.position = Vector3.Lerp (observed_transform.position, server_pos, lerp);
		}
	}
	
	public void OnSerializeNetworkViewBall(BitStream stream, NetworkMessageInfo info)
	{
		Vector3 pos = observed_transform.position;
		Vector3 velocity = observed_transform.rigidbody.velocity;
		
		if (stream.isWriting) {
		
			stream.Serialize(ref pos);
			stream.Serialize(ref velocity);
			
		} else {
			
			//This code takes care of the local client
			stream.Serialize(ref pos);
			stream.Serialize(ref velocity);
			server_pos = pos;
			
			// smoothly correct clients position
			LerpToTarget();
			
			// Take care of data for interpolating remote objects movements
			// Shift up the buffer
			for (int i = server_state_buffer.Length-1; i >= 1; i--)
				server_state_buffer[i] = server_state_buffer[i-1];
			
			//Override the first element with the latest server info
			server_state_buffer[0] = new NetState((float)info.timestamp, pos, velocity);
		}
	}
	
	public Transform getObservedTransform()
	{
		return observed_transform;
	}
	
	public void Predict(NetworkView networkView)
	{
		
		if (Network.player == networkView.owner || Network.isServer) {
			return; //This is only for remote peers, get off!!
		}
		
		//client side has **only the server connected**
		client_ping = (Network.GetAveragePing(Network.connections[0]) / 100) + PING_MARGIN;
		
		float interpolation_time = (float)Network.time - client_ping;
		
		//ensure the buffer has at last one element
		if (server_state_buffer[0] == null)
			server_state_buffer[0] = new NetState(0, observed_transform.position, observed_transform.rigidbody.velocity);
		
		
		Debug.Log(Network.time - server_state_buffer[0].timestamp);
		

		NetState latest = server_state_buffer[0];
		if(!latest.state_used) {
			observed_transform.position = Vector3.Lerp(observed_transform.position, latest.pos, 0.5f);
			observed_transform.rigidbody.velocity = latest.velocity;
			server_state_buffer[0].state_used = true;
			
			
			float x,y,z;
		
			x = server_state_buffer[0].pos.x + server_state_buffer[0].velocity.x*((float)Network.time - server_state_buffer[0].timestamp);
			y = server_state_buffer[0].pos.y + server_state_buffer[0].velocity.y*((float)Network.time - server_state_buffer[0].timestamp);
			z = server_state_buffer[0].pos.z + server_state_buffer[0].velocity.z*((float)Network.time - server_state_buffer[0].timestamp);
			
			RaycastHit hit;
			Vector3 predicted_pos = new Vector3(x,y,z);
			Vector3 direction = predicted_pos;
			direction.Normalize();
			
			float distance = Vector3.Distance(latest.pos, predicted_pos);
			
			if(distance!=0 && Physics.Raycast(latest.pos, direction, out hit, Mathf.Abs(distance))) {
			
				direction = direction*(-1);
				x = hit.point.x + direction.x*((SphereCollider)observed_transform.collider).radius;
				y = hit.point.y + direction.y*((SphereCollider)observed_transform.collider).radius;
				z = hit.point.z + direction.z*((SphereCollider)observed_transform.collider).radius;

				
			}

			observed_transform.position =  Vector3.Lerp (observed_transform.position, new Vector3(x,y,z), 0.25f);
		}
	}
	
}
