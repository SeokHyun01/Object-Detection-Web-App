function get1dGaussianKernel(sigma, size) {
    // Generate a 1d gaussian distribution across a range
    var x = tf.range(Math.floor(-size / 2) + 1, Math.floor(size / 2) + 1)
    x = tf.pow(x, 2)
    x = tf.exp(tf.div(x, -2.0 * (sigma * sigma)))
    x = tf.div(x, tf.sum(x))
    return x
}

function get2dGaussianKernel(size, sigma) {
    // This default is to mimic opencv2. 
    sigma = sigma || (0.3 * ((size - 1) * 0.5 - 1) + 0.8)

    var kerne1d = get1dGaussianKernel(sigma, size)
    return tf.outerProduct(kerne1d, kerne1d)
}

function getGaussianKernel(size = 5, sigma) {
    return tf.tidy(() => {
        var kerne2d = get2dGaussianKernel(size, sigma)
        var kerne3d = tf.stack([kerne2d, kerne2d, kerne2d])
        return tf.reshape(kerne3d, [size, size, 3, 1])
    })
}

function blur(image, kernel) {
    return tf.tidy(() => {
        return tf.depthwiseConv2d(image, kernel, 1, "valid")
    })
}