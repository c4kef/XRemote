namespace OpenWeatherAPI
{
	public static class API
	{
		//Returns null if invalid request
		public static Query Query(string city)
		{
			Query newQuery = new Query(city);
			if (newQuery.ValidRequest)
				return newQuery;
			return null;
		}
	}
}
