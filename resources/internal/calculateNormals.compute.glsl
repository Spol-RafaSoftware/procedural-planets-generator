#version 430
[ComputeShader]


uniform int param_indiciesCount;

struct vec3_struct
{
    float x;
    float y;
    float z;
};
struct vec2_struct
{
    float x;
    float y;
    float z;
};

layout( binding=0 ) buffer buffer1 {
    vec3_struct positions[];
};
layout( binding=1 ) buffer buffer2 {
    vec3_struct normals[];
};
layout( binding=2 ) buffer buffer3 {
    vec2_struct uvs[];
};
layout( binding=3 ) buffer buffer4 {
    int indicies[];
};


layout(local_size_x = 1, local_size_y = 1, local_size_z = 1) in;
 

vec3 fromStruct(vec3_struct p)
{
	return vec3(p.x, p.y, p.z);
}
vec3_struct toStruct(vec3 p)
{
	vec3_struct s;
	s.x = p.x;
	s.y = p.y;
	s.z = p.z;
	return s;
}


vec3 GetNormal(int a, int b, int c)
{
	vec3 _a = fromStruct(positions[a]);
	vec3 _b = fromStruct(positions[b]);
	vec3 _c = fromStruct(positions[c]);
	return normalize(cross(normalize(_b - _a), normalize(_c - _a)));
}


void main() {
	
	int id = int(gl_GlobalInvocationID.x);	

	vec3 normal = vec3(0);
	int normalCount = 0;

	for(int i=0; i<indiciesCount; i+=3)
	{
		int a = indicies[i];
		int b = indicies[i+1];
		int c = indicies[i+2];
		if(a == id || b == id || c== id) {
			normal += GetNormal(a,b,c);
			normalCount++;
		}
	}

	normal /= normalCount;
	normals[id] = toStruct(normal);
}

