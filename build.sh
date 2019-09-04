#!/usr/bin/env bash
set -e

DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

echo ""

if [ $# -gt 1 -a "$1" == "push" ]
then
    TAG=$2

    echo "Pushing Bitwarden ($TAG)"
    echo "========================"
    
    docker push bitwardence/api:$TAG
    docker push bitwardence/identity:$TAG
    docker push bitwardence/server:$TAG
    docker push bitwardence/attachments:$TAG
    docker push bitwardence/icons:$TAG
    docker push bitwardence/notifications:$TAG
    docker push bitwardence/events:$TAG
    docker push bitwardence/admin:$TAG
    docker push bitwardence/nginx:$TAG
    docker push bitwardence/mssql:$TAG
    docker push bitwardence/setup:$TAG
elif [ $# -gt 1 -a "$1" == "tag" ]
then
    TAG=$2
    
    echo "Tagging Bitwarden as '$TAG'"
    
    docker tag bitwardence/api bitwardence/api:$TAG
    docker tag bitwardence/identity bitwardence/identity:$TAG
    docker tag bitwardence/server bitwardence/server:$TAG
    docker tag bitwardence/attachments bitwardence/attachments:$TAG
    docker tag bitwardence/icons bitwardence/icons:$TAG
    docker tag bitwardence/notifications bitwardence/notifications:$TAG
    docker tag bitwardence/events bitwardence/events:$TAG
    docker tag bitwardence/admin bitwardence/admin:$TAG
    docker tag bitwardence/nginx bitwardence/nginx:$TAG
    docker tag bitwardence/mssql bitwardence/mssql:$TAG
    docker tag bitwardence/setup bitwardence/setup:$TAG
else
    echo "Building Bitwarden"
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
