docker build -f "RatesCollector\Dockerfile" --force-rm -t ratescollector  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=RatesCollector" .
docker build -f "StatisticsCollector\Dockerfile" --force-rm -t statisticscollector  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=StatisticsCollector" .
docker build -f "RatesGatwewayApi\Dockerfile" --force-rm -t ratesgatewayapi  --label "com.microsoft.created-by=visual-studio" --label "com.microsoft.visual-studio.project-name=RatesGatwewayApi" .
