/// <binding BeforeBuild='build' Clean='clean' ProjectOpened='build' />

const gulp = require('gulp'),
    rimraf = require('rimraf'),
    merge = require('merge-stream'),
    googleWebFonts = require('gulp-google-webfonts'),
    sass = require('gulp-sass');

const paths = {};
paths.webroot = './wwwroot/';
paths.npmDir = './node_modules/';
paths.sassDir = './Sass/';
paths.libDir = paths.webroot + 'lib/';
paths.cssDir = paths.webroot + 'css/';
paths.jsDir = paths.webroot + 'js/';

paths.sass = paths.sassDir + '**/*.scss';
paths.minCss = paths.cssDir + '**/*.min.css';
paths.js = paths.jsDir + '**/*.js';
paths.minJs = paths.jsDir + '**/*.min.js';
paths.libJs = paths.libDir + '**/*.js';
paths.libMinJs = paths.libDir + '**/*.min.js';

const cleaner = path => (cb) => rimraf(path, cb);

const clean_js = cleaner(paths.minJs);
const clean_css = cleaner(paths.cssDir);
const clean_lib = cleaner(paths.libDir);

const build_lib = gulp.series(clean_lib, () => {
    const libs = [
        {
            src: paths.npmDir + 'bootstrap/dist/js/*',
            dest: paths.libDir + 'bootstrap/js'
        },
        {
            src: paths.npmDir + 'popper.js/dist/umd/*',
            dest: paths.libDir + 'popper'
        },
        {
            src: paths.npmDir + 'font-awesome/css/*',
            dest: paths.libDir + 'font-awesome/css'
        },
        {
            src: paths.npmDir + 'font-awesome/fonts/*',
            dest: paths.libDir + 'font-awesome/fonts'
        },
        {
            src: paths.npmDir + 'jquery/dist/jquery.slim*',
            dest: paths.libDir + 'jquery'
        },
    ];

    const tasks = libs.map((lib) => {
        return gulp.src(lib.src).pipe(gulp.dest(lib.dest));
    });
    return merge(tasks);
});

const build_sass = () => {
    return gulp.src(paths.sass)
        .pipe(sass({ outputStyle: 'compressed' }).on('error', sass.logError))
        .pipe(gulp.dest(paths.cssDir));
};

const build_webfonts = () => {
    return gulp.src('./webfonts.list')
        .pipe(googleWebFonts({
            fontsDir: 'webfonts',
            cssFilename: 'webfonts.css'
        }))
        .pipe(gulp.dest(paths.cssDir));
};

exports.clean = gulp.parallel(clean_js, clean_css, clean_lib);
exports.build = gulp.parallel(build_lib, build_sass, build_webfonts);