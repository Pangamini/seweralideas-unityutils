float3x3 AxisAngleToMatrix(float3 vec, float radians)
{
	float s = sin(radians);
	float c = cos(radians);

	float vx = vec.x;
	float vy = vec.y;
	float vz = vec.z;

	float xx = vx * vx;
	float yy = vy * vy;
	float zz = vz * vz;
	float xy = vx * vy;
	float yz = vy * vz;
	float zx = vz * vx;
	float xs = vx * s;
	float ys = vy * s;
	float zs = vz * s;
	float one_c = 1.0f - c;

	float3x3 result;

	result[0][0] = (one_c * xx) + c;
	result[1][0] = (one_c * xy) - zs;
	result[2][0] = (one_c * zx) + ys;

	result[0][1] = (one_c * xy) + zs;
	result[1][1] = (one_c * yy) + c;
	result[2][1] = (one_c * yz) - xs;

	result[0][2] = (one_c * zx) - ys;
	result[1][2] = (one_c * yz) + xs;
	result[2][2] = (one_c * zz) + c;

	return result;
}

float4x4 PositionAndEulerToMatrix(float3 p, float3 v)
{
	float cx = cos(v.x);
	float sx = sin(v.x);
	float cy = cos(v.y);
	float sy = sin(v.y);
	float cz = cos(-v.z);
	float sz = sin(-v.z);

	float4x4 result;

	result[0][0] = cy * cz + sx * sy * sz;
	result[0][1] = cz * sx * sy - cy * sz;
	result[0][2] = cx * sy;
	result[0][3] = 0.0f;

	result[1][0] = cx * sz;
	result[1][1] = cx * cz;
	result[1][2] = -sx;
	result[1][3] = 0.0f;

	result[2][0] = -cz * sy + cy * sx * sz;
	result[2][1] = cy * cz * sx + sy * sz;
	result[2][2] = cx * cy;
	result[2][3] = 0.0f;

	result[3][0] = p.x;
	result[3][1] = p.y;
	result[3][2] = p.z;
	result[3][3] = 1.0f;

	return result;
}