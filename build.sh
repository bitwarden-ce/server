#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo ""

if [ $# -gt 1 -a "$1" == "push" ]
then
    TAG=$2

    echo "Pushing ByteGarden ($TAG)"
    echo "========================"
    
    docker push bytegarden/api:$TAG
    docker push bytegarden/identity:$TAG
    docker push bytegarden/server:$TAG
    docker push bytegarden/attachments:$TAG
    docker push bytegarden/icons:$TAG
    docker push bytegarden/notifications:$TAG
    docker push bytegarden/events:$TAG
    docker push bytegarden/admin:$TAG
    docker push bytegarden/nginx:$TAG
    docker push bytegarden/mssql:$TAG
    docker push bytegarden/setup:$TAG
elif [ $# -gt 1 -a "$1" == "tag" ]
then
    TAG=$2
    
    echo "Tagging ByteGarden as '$TAG'"
    
    docker tag bytegarden/api bytegarden/api:$TAG
    docker tag bytegarden/identity bytegarden/identity:$TAG
    docker tag bytegarden/server bytegarden/server:$TAG
    docker tag bytegarden/attachments bytegarden/attachments:$TAG
    docker tag bytegarden/icons bytegarden/icons:$TAG
    docker tag bytegarden/notifications bytegarden/notifications:$TAG
    docker tag bytegarden/events bytegarden/events:$TAG
    docker tag bytegarden/admin bytegarden/admin:$TAG
    docker tag bytegarden/nginx bytegarden/nginx:$TAG
    docker tag bytegarden/mssql bytegarden/mssql:$TAG
    docker tag bytegarden/setup bytegarden/setup:$TAG
else
    echo "Building ByteGarden"
    echo "=================="

    chmod u+x $DIR/src/Api/build.sh
    $DIR/src/Api/build.sh

    chmod u+x $DIR/src/Identity/build.sh
    $DIR/src/Identity/build.sh

    chmod u+x $DIR/util/Server/build.sh
    $DIR/util/Server/build.sh

    chmod u+x $DIR/util/Nginx/build.sh
    $DIR/util/Nginx/build.sh

    chmod u+x $DIR/util/Attachments/build.sh
    $DIR/util/Attachments/build.sh

    chmod u+x $DIR/src/Icons/build.sh
    $DIR/src/Icons/build.sh

    chmod u+x $DIR/src/Notifications/build.sh
    $DIR/src/Notifications/build.sh

    chmod u+x $DIR/src/Events/build.sh
    $DIR/src/Events/build.sh

    chmod u+x $DIR/src/Admin/build.sh
    $DIR/src/Admin/build.sh

    chmod u+x $DIR/util/MsSql/build.sh
    $DIR/util/MsSql/build.sh

    chmod u+x $DIR/util/Setup/build.sh
    $DIR/util/Setup/build.sh
fi
